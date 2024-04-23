namespace WebApi.Configs;

public class KafkaConfig
{
    public string BootstrapServers { get; set; }
    public string SecurityProtocol { get; set; }
    public string SaslMechanisms { get; set; }
    public string SaslUsername { get; set; }
    public string SaslPassword { get; set; }
    public int SessionTimeoutMs { get; set; }
}