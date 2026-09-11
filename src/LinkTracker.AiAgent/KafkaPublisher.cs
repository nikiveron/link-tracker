using Confluent.Kafka;
using LinkTracker.AiAgent.Domain;
using LinkTracker.Contracts;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LinkTracker.AiAgent;

public class KafkaPublisher : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _outputTopic;

    public KafkaPublisher(IOptions<KafkaOptions> kafkaOptions)
    {
        var options = kafkaOptions.Value;
        if (string.IsNullOrEmpty(options.BootstrapServers)) { throw new InvalidOperationException("Kafka:BootstrapServers is not configured"); }
        if (string.IsNullOrEmpty(options.OutputTopic)) { throw new InvalidOperationException("Kafka:OutputTopic is not configured"); }

        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _outputTopic = options.OutputTopic;
    }

    public Task Produce(LinkUpdateDto link, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(link);
        return _producer.ProduceAsync(
            _outputTopic,
            new Message<string, string> { Value = json },
            ct);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
