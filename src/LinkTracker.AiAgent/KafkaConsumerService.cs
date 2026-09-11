using Confluent.Kafka;
using LinkTracker.AiAgent.Domain;
using LinkTracker.AiAgent.Services;
using LinkTracker.Contracts;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LinkTracker.AiAgent;

public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly IGroupingService _groupingService;
    private readonly IPrepareMessageService _prepareMessageService;
    private readonly GroupingOptions _groupingOptions;
    private readonly PeriodicTimer _flushTimer;

    public KafkaConsumerService(
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<AiAgentOptions> aiAgentOptions,
        ILogger<KafkaConsumerService> logger,
        IGroupingService groupingService,
        IPrepareMessageService prepareMessageService)
    {
        _logger = logger;
        _groupingService = groupingService;
        _prepareMessageService = prepareMessageService;
        _groupingOptions = aiAgentOptions.Value.Grouping;

        var options = kafkaOptions.Value;

        if (string.IsNullOrEmpty(options.BootstrapServers)) { throw new InvalidOperationException("Kafka:BootstrapServers is not configured"); }
        if (string.IsNullOrEmpty(options.InputTopic)) { throw new InvalidOperationException("Kafka:InputTopic is not configured"); }

        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = "ai-agent-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(options.InputTopic);

        _flushTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_groupingOptions.WindowMs / 2));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var consumerTask = Task.Run(() => ConsumeLoop(cancellationToken), cancellationToken);
        var flushTask = Task.Run(() => FlushLoop(cancellationToken), cancellationToken);

        await Task.WhenAll(consumerTask, flushTask);
    }

    private async Task ConsumeLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka consumer запущен");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(cancellationToken);
                var update = JsonSerializer.Deserialize<LinkUpdateDto>(result.Message.Value);

                await _prepareMessageService.Execute(update, cancellationToken);

                _consumer.Commit(result);
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
    }

    private async Task FlushLoop(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запущен таймер группировки (окно={WindowMs}мс)", _groupingOptions.WindowMs);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _flushTimer.WaitForNextTickAsync(cancellationToken);
                await _groupingService.FlushReadyMessages(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке сгруппированных сообщений");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка Kafka consumer...");

        await _groupingService.FlushReadyMessages(cancellationToken);

        _consumer?.Close();
        _consumer?.Dispose();
        _flushTimer?.Dispose();

        await base.StopAsync(cancellationToken);
    }
}
