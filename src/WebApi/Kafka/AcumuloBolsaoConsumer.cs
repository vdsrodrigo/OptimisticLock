using Confluent.Kafka;
using WebApi.Configs;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Kafka
{
    public class AcumuloBolsaoConsumer : BackgroundService
    {
        private readonly AcumuloContext _db;
        private readonly KafkaConfig _kafkaConfig;
        private readonly ILogger<AcumuloBolsaoConsumer> _logger;
        private IConsumer<string, string> _consumer;
        private IProducer<string, string> _producer;

        public AcumuloBolsaoConsumer(AcumuloContext db, ILogger<AcumuloBolsaoConsumer> logger, KafkaConfig kafkaConfig)
        {
            _logger = logger;
            _kafkaConfig = kafkaConfig;
            _db = db;

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
            _consumer.Subscribe("acumulo_pontos_bolsao");

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
                            _logger.LogInformation("Atualizando o valor do acumulador no primeiro registro do banco de dados");
                            const int maximoDeTentativas = 20;
                            var totalDeConflitos = 0;
                            for (int tentativa = 0; tentativa < maximoDeTentativas; tentativa++)
                            {
                                try
                                {
                                    var shell = await _db.AcumuladorShell.FindAsync(Guid.Parse("65388a8e-805e-48ba-9bdd-587bf6465e8e"));
                                    if (shell is null)
                                    {
                                        _logger.LogInformation("Registro não encontrado");
                                        return;
                                    }

                                    shell.Valor += Convert.ToInt32(consumeResult.Message.Value);
                                    shell.RowVersion += 1;

                                    await _db.SaveChangesAsync(stoppingToken);
                                    _logger.LogInformation("Registro atualizado com sucesso");

                                    _logger.LogInformation($"Quantidade de conflitos ocorridas :{totalDeConflitos}");
                                    return;
                                }
                                catch (DbUpdateConcurrencyException) when (tentativa < maximoDeTentativas - 1)
                                {
                                    _logger.LogInformation($"Total de conflitos: {totalDeConflitos++}");
                                }
                            }

                            Console.WriteLine(totalDeConflitos);
                            _logger.LogInformation($"Quantidade de conflitos ocorridas :{totalDeConflitos}");
                            return;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message");

                            var dlqMessage = new Message<string, string>
                            {
                                Key = consumeResult.Message.Key,
                                Value = consumeResult.Message.Value
                            };

                            _producer.Produce("acumulo_pontos_bolsao_dlq", dlqMessage);
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