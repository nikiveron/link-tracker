using Confluent.Kafka;
using LinkTracker.Contracts;
using LinkTracker.Scrapper.Application.Interfaces;
using System.Text.Json;

namespace LinkTracker.Scrapper.Infrastructure.Clients;

public class KafkaNotificationSender : INotificationSender
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaNotificationSender(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topic = configuration["Kafka:Topic"] ?? throw new ArgumentNullException(_topic, "Ошибка! Конфигурация Kafka:Topic не заполнен!");
    }

    public async Task SendAsync(LinkUpdateDto update, CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = update.ChatId.ToString(),
            Value = JsonSerializer.Serialize(update)
        };

        await _producer.ProduceAsync(_topic, message, cancellationToken);
    }
}
