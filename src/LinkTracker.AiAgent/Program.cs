using LinkTracker.AiAgent;
using LinkTracker.AiAgent.Domain;
using LinkTracker.AiAgent.Services;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddOptions<AiAgentOptions>()
    .Bind(builder.Configuration.GetSection(AiAgentOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<KafkaOptions>()
    .Bind(builder.Configuration.GetSection(KafkaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<KafkaPublisher>();
builder.Services.AddSingleton<IPriorityService, PriorityService>();
builder.Services.AddSingleton<IGroupingService, GroupingService>();
builder.Services.AddSingleton<IPrepareMessageService, PrepareMessageService>();
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddMetrics();

var app = builder.Build();

app.UseHttpMetrics();

app.MapMetrics("/metrics");

app.Run();
