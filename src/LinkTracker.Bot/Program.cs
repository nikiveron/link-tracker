using LinkTracker.Bot;
using LinkTracker.Bot.Application.Commands;
using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Bot.Application.Services;
using LinkTracker.Bot.Application.StateMachine;
using LinkTracker.Bot.Application.StateMachine.States;
using LinkTracker.Bot.Application.StateMachine.States.ListStates;
using LinkTracker.Bot.Application.StateMachine.States.TrackStates;
using LinkTracker.Bot.Application.StateMachine.States.UnTrackStates;
using LinkTracker.Bot.Infrastructure.BotExtensions;
using LinkTracker.Bot.Infrastructure.Client;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using Prometheus;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

LoadEnvs.Run();
builder.Configuration.AddEnvironmentVariables();

var configuration = builder.Configuration;

builder.Services.AddHealthChecks();

#region Telegram

builder.Services.Configure<BotOptions>(configuration.GetSection("TG"));

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<BotOptions>>().Value;
    return new TelegramBotClient(options.Token);
});

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
            failureThreshold,
            samplingDuration,
            minimumThroughput,
            breakDuration,
            onBreak: (result, state, timespan, context) =>
            {
                Console.WriteLine($"[CircuitBreaker] OPEN for {timespan}");
            },
            onReset: context =>
            {
                Console.WriteLine("[CircuitBreaker] CLOSED");
            },
            onHalfOpen: () =>
            {
                Console.WriteLine("[CircuitBreaker] HALF-OPEN");
            });

#endregion

#region HttpClient → Scrapper

var scrapperBaseUrl =
    configuration.GetValue<string>("Scrapper:BaseUrl") ?? "http://scrapper:8080";

builder.Services.AddHttpClient<IScrapperClient, ScrapperClient>(client =>
{
    client.BaseAddress = new Uri(scrapperBaseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "LinkTracker-Bot");
})
.AddPolicyHandler(timeoutPolicy)
.AddPolicyHandler(retryPolicy)
.AddPolicyHandler(circuitBreakerPolicy);

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
        opt.QueueLimit = 0;
    });
});

#endregion

#region Kafka Consumer

builder.Services.AddHostedService<KafkaConsumerService>();

#endregion

#region Commands & State Machine

builder.Services.AddSingleton<StartCommand>();
builder.Services.AddSingleton<HelpCommand>();
builder.Services.AddSingleton<TrackCommand>();
builder.Services.AddSingleton<UnTrackCommand>();
builder.Services.AddSingleton<ListCommand>();
builder.Services.AddSingleton<CancelCommand>();
builder.Services.AddSingleton<UnknownCommand>();
builder.Services.AddSingleton<BotCommands>();

builder.Services.AddSingleton<ICommand>(sp => sp.GetRequiredService<StartCommand>());
builder.Services.AddSingleton<ICommand>(sp => sp.GetRequiredService<HelpCommand>());
builder.Services.AddSingleton<ICommand>(sp => sp.GetRequiredService<TrackCommand>());
builder.Services.AddSingleton<ICommand>(sp => sp.GetRequiredService<UnTrackCommand>());
builder.Services.AddSingleton<ICommand>(sp => sp.GetRequiredService<ListCommand>());
builder.Services.AddSingleton<ICommand>(sp => sp.GetRequiredService<CancelCommand>());

builder.Services.AddSingleton<CommandDispatcher>();

builder.Services.AddSingleton<DefaultState>();
builder.Services.AddSingleton<WaitingForAddLinkState>();
builder.Services.AddSingleton<WaitingForTagsState>();
builder.Services.AddSingleton<WaitingForTagsConfirmationState>();
builder.Services.AddSingleton<WaitingForDeleteLinkState>();
builder.Services.AddSingleton<WaitingForFilterTagsConfirmationState>();
builder.Services.AddSingleton<WaitingForFilterTagsState>();

builder.Services.AddSingleton<StateFactory>();
builder.Services.AddSingleton<UserStateManager>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUpdateValidationService, UpdateValidationService>();
builder.Services.AddSingleton<IBotMetricsService, BotMetricsService>();

#endregion

builder.Services.AddMetrics();

builder.Services.AddControllers();

var app = builder.Build();

app.UseRateLimiter();

app.UseHttpMetrics();

app.MapHealthChecks("/health");
app.MapControllers().RequireRateLimiting("fixed");

app.MapMetrics("/metrics");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
var userStateManager = app.Services.GetRequiredService<UserStateManager>();
var botCommands = app.Services.GetRequiredService<BotCommands>();

await botCommands.SetCommands(botClient);

using var cts = new CancellationTokenSource();

botClient.StartReceiving(
    async (client, update, token) =>
    {
        if (update.CallbackQuery != null)
        {
            var chatId = update.CallbackQuery.Message!.Chat.Id;
            var user = userStateManager.GetUser(chatId, token);
            await user.HandleCallback(update.CallbackQuery, token);
            return;
        }

        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var user = userStateManager.GetUser(chatId, token);
            await user.HandleMessage(update, token);
        }
    },
    (client, exception, token) =>
    {
        logger.LogError("Telegram error: {message}", exception.Message);
    },
    cancellationToken: cts.Token);

logger.LogInformation("Bot started...");

app.Run();