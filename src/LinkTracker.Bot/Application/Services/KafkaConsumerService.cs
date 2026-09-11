using Confluent.Kafka;
using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Contracts;
using System.Text.Json;

namespace LinkTracker.Bot.Application.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;

    public KafkaConsumerService(IConfiguration configuration, IServiceScopeFactory serviceScopeFactory, ILogger<KafkaConsumerService> logger)
    {
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = "bot-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_configuration["Kafka:Topic"]);

        return Task.Run(() =>
        {
            _logger.LogInformation("Kafka consumer запущен, слушает топик {Topic}", _configuration["Kafka:Topic"]);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(ct);
                    var update = JsonSerializer.Deserialize<LinkUpdateDto>(result.Message.Value);

                    using var scope = _serviceScopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    notificationService.NotifyUsersAsync(update, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке сообщения Kafka");
                }
            }
            _logger.LogInformation("Kafka consumer остановлен");
        }, ct);
    }
}
