using Confluent.Kafka;
using WebApi.Configs;
using MongoDB.Driver;

namespace WebApi.Kafka
{
    public class AcumuloConsumer : BackgroundService
    {
        private readonly KafkaConfig _kafkaConfig;
        private readonly ILogger<AcumuloConsumer> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly IProducer<string, string> _producer;
        private readonly AcumuloContext _db;
        private readonly IMongoCollection<ShellRepository> _mongoCollection;

        public AcumuloConsumer(AcumuloContext db, ILogger<AcumuloConsumer> logger, KafkaConfig kafkaConfig, IConfiguration configuration)
        {
            _logger = logger;
            _kafkaConfig = kafkaConfig;
            _db = db;
            
            var mongoClient = new MongoClient(configuration.GetConnectionString("MongoDB"));
            var mongoDatabase = mongoClient.GetDatabase("myDatabase");
            _mongoCollection = mongoDatabase.GetCollection<ShellRepository>("acumuladorShell");

            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                SecurityProtocol = kafkaConfig.SecurityProtocol,
                SaslMechanism = kafkaConfig.SaslMechanisms,
                SaslUsername = _kafkaConfig.SaslUsername,
                SaslPassword = _kafkaConfig.SaslPassword,
                GroupId = "acumulo_pontos_group",
                AutoOffsetReset = AutoOffsetReset.Latest
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
                            _logger.LogInformation("Adicionando um novo registro no banco de dados");

                            var shellRepository = new ShellRepository
                                { Nome = "Shell", Valor = Convert.ToInt32(consumeResult.Message.Value), RowVersion = 1 };

                            // inserindo no postgres
                            _db.Add(shellRepository);
                            await _db.SaveChangesAsync(stoppingToken);

                            // inserindo no mongoDB
                            await _mongoCollection.InsertOneAsync(shellRepository, cancellationToken:stoppingToken);
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

                await Task.Delay(10, stoppingToken);
            }

            _consumer.Close();
            _producer.Flush(TimeSpan
                .FromSeconds(10));

            _producer.Dispose();
        }
    }
}