using LinkTracker.Scrapper;
using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Application.Services;
using LinkTracker.Scrapper.Domain.Options;
using LinkTracker.Scrapper.Infrastructure.Clients;
using LinkTracker.Scrapper.Infrastructure.Context;
using LinkTracker.Scrapper.Infrastructure.Scheduler;
using LinkTracker.Scrapper.Infrastructure.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Prometheus;
using Quartz;
using StackExchange.Redis;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

LoadEnvs.Run();
builder.Configuration.AddEnvironmentVariables();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var configuration = builder.Configuration;

#region Database

string connection = configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<ChatsContext>(options => options.UseNpgsql(connection));

var accessType = configuration["Database:AccessType"];

if (accessType == "SQL")
{
    builder.Services.AddScoped<ILinkRepository>(_ => new SqlLinkRepository(connection));
}
else
{
    builder.Services.AddScoped<ILinkRepository, OrmLinkRepository>();
}

#endregion

#region Redis (Valkey)

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var config = configuration["Valkey:Configuration"]
                 ?? throw new ArgumentNullException(configuration["Valkey:Configuration"], "Ошибка! Valkey конфигурация не настроена.");
    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddSingleton<ICacheService, ValkeyCacheService>();

#endregion

#region Resilience Configuration

var timeoutSeconds = configuration.GetValue<int>("Resilience:TimeoutSeconds");
var retryCount = configuration.GetValue<int>("Resilience:RetryCount");
var retryDelayMs = configuration.GetValue<int>("Resilience:RetryDelayMilliseconds");
var retryableStatusCodes = configuration
    .GetSection("Resilience:RetryableStatusCodes")
    .Get<int[]>() ?? [];

var cbSection = configuration.GetSection("Resilience:CircuitBreaker");

var failureThreshold = cbSection.GetValue<double>("FailureRateThreshold");
var samplingDuration = TimeSpan.FromSeconds(cbSection.GetValue<int>("SamplingDurationSeconds"));
var minimumThroughput = cbSection.GetValue<int>("MinimumThroughput");
var breakDuration = TimeSpan.FromSeconds(cbSection.GetValue<int>("BreakDurationSeconds"));

#endregion

#region Polly Policies

IAsyncPolicy<HttpResponseMessage> timeoutPolicy =
    Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(timeoutSeconds));

IAsyncPolicy<HttpResponseMessage> retryPolicy =
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TimeoutRejectedException>()
        .OrResult(r => retryableStatusCodes.Contains((int)r.StatusCode))
        .WaitAndRetryAsync(
            retryCount,
            _ => TimeSpan.FromMilliseconds(retryDelayMs)
        );

IAsyncPolicy<HttpResponseMessage> circuitBreakerPolicy =
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .Or<TimeoutRejectedException>()
        .OrResult(r => retryableStatusCodes.Contains((int)r.StatusCode))
        .AdvancedCircuitBreakerAsync(
            failureThreshold: failureThreshold,
            samplingDuration: samplingDuration,
            minimumThroughput: minimumThroughput,
            durationOfBreak: breakDuration,
            onBreak: (result, timespan) =>
            {
                Console.WriteLine($"Circuit OPEN for {timespan.TotalSeconds} sec");
            },
            onReset: () =>
            {
                Console.WriteLine("Circuit CLOSED");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("Circuit HALF-OPEN");
            });

#endregion

#region HTTP Clients (GitHub, StackOverflow)

builder.Services.Configure<GitHubOptions>(configuration.GetSection("GitHub"));

builder.Services.AddHttpClient<IGitHubClient, GitHubClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<GitHubOptions>>().Value;

    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "LinkTracker-Bot");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Token);
})
.AddPolicyHandler(timeoutPolicy)
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

builder.Services.Configure<StackOverflowOptions>(configuration.GetSection("StackOverflow"));

builder.Services.AddHttpClient<IStackOverflowClient, StackOverflowClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<StackOverflowOptions>>().Value;

    client.DefaultRequestHeaders.Add("User-Agent", "LinkTracker-Bot");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Token);
})
    .AddPolicyHandler(timeoutPolicy)
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

#endregion

#region Notification Senders (HTTP + Kafka Fallback)

builder.Services.AddHttpClient<BotClientNotificationSender>()
    .AddPolicyHandler(timeoutPolicy)
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

builder.Services.AddSingleton<KafkaNotificationSender>();
builder.Services.AddSingleton<KafkaNotificationSender>();

builder.Services.AddScoped<INotificationSender>(sp =>
{
    var mode = configuration["Notification:Mode"];
    var httpSender = sp.GetRequiredService<BotClientNotificationSender>();
    var kafkaSender = sp.GetRequiredService<KafkaNotificationSender>();

    if (mode == "Http")
    {
        return new FallbackNotificationSender(httpSender, kafkaSender);
    }

    return kafkaSender;
});

#endregion

#region Rate Limiting

var permitLimit = configuration.GetValue<int>("RateLimiting:PermitLimit");
var windowSeconds = configuration.GetValue<int>("RateLimiting:WindowSeconds");

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = permitLimit;
        opt.Window = TimeSpan.FromSeconds(windowSeconds);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

#endregion

#region Application Services

builder.Services.AddScoped<IUpdateCheckerService, UpdateCheckerService>();
builder.Services.AddScoped<IMessageFormatter, MessageFormatter>();
builder.Services.AddScoped<ILinkRequestValidator, LinkRequestValidator>();
builder.Services.AddScoped<IChatValidationService, ChatValidationService>();
builder.Services.AddSingleton<IMetricsService, MetricsService>();

#endregion

#region Quartz

builder.Services.Configure<ScrapperOptions>(configuration.GetSection("ScrapperOptions"));

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("link-check");

    q.AddJob<LinkCheckJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("link-check-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInSeconds(configuration.GetValue<int>("ScrapperOptions:SchedulerIntervalSeconds"))
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

#endregion

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddMetrics();

var app = builder.Build();

app.UseRateLimiter();

app.UseHttpMetrics();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers().RequireRateLimiting("fixed");
app.MapHealthChecks("/health");

app.MapMetrics("/metrics");

app.Run();
