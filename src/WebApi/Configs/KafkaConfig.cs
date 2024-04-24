using Confluent.Kafka;

namespace WebApi.Configs;

public class KafkaConfig
{
    public string BootstrapServers { get; set; }
    public SecurityProtocol? SecurityProtocol { get; set; }
    public SaslMechanism? SaslMechanisms { get; set; }
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public int SessionTimeoutMs { get; set; }
}