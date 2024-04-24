using Confluent.Kafka;
using WebApi.Configs;

namespace WebApi.Kafka;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly KafkaConfig _kafkaConfig;
    private IConsumer<string, string> _consumer;

    public KafkaConsumerBackgroundService(ILogger<KafkaConsumerBackgroundService> logger, KafkaConfig kafkaConfig)
    {
        _logger = logger;
        _kafkaConfig = kafkaConfig;

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaConfig.BootstrapServers,
            SecurityProtocol = kafkaConfig.SecurityProtocol,
            SaslMechanism = kafkaConfig.SaslMechanisms,
            SaslUsername = _kafkaConfig.SaslUsername,
            SaslPassword = _kafkaConfig.SaslPassword,
            GroupId = "acumulo_pontos_group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("acumulo_pontos");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Run(() =>
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    _logger.LogInformation(
                        $"Consumed message '{consumeResult.Message.Value}' at: '{consumeResult.TopicPartitionOffset}'.");
                }, stoppingToken);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message");
            }

            await Task.Delay(1000, stoppingToken); // wait for 1 second before next iteration
        }

        _consumer.Close();
    }
}