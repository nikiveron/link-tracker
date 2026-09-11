namespace LinkTracker.AiAgent.Domain;

public class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;
    public string InputTopic { get; set; } = string.Empty;
    public string OutputTopic { get; set; } = string.Empty;
}
