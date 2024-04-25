using Confluent.Kafka;
using WebApi.Configs;

namespace WebApi.Kafka
{
    public class AcumuloConsumer : BackgroundService
    {
        private readonly KafkaConfig _kafkaConfig;
        private readonly ILogger<AcumuloConsumer> _logger;
        private IConsumer<string, string> _consumer;
        private IProducer<string, string> _producer;

        public AcumuloConsumer(ILogger<AcumuloConsumer> logger, KafkaConfig kafkaConfig)
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
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe("acumulo_pontos");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult != null)
                    {
                        try
                        {
                            _logger.LogInformation($"Received message: {consumeResult.Message.Value}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message");

                            var dlqMessage = new Message<string, string>
                            {
                                Key = consumeResult.Message.Key,
                                Value = consumeResult.Message.Value
                            };

                            _producer.Produce("acumulo_pontos_dlq", dlqMessage);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message");
                }

                await Task.Delay(1000, stoppingToken);
            }

            _consumer.Close();
            _producer.Flush(TimeSpan
                .FromSeconds(10));

            _producer.Dispose();
        }
    }
}