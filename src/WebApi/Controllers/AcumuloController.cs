using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using WebApi.Configs;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/acumulo")]
    public class AcumuloController : ControllerBase
    {
        private readonly ILogger<AcumuloController> _logger;
        private readonly IProducer<string, string> _producer;
        private readonly KafkaConfig _kafkaConfig;

        public AcumuloController(KafkaConfig kafkaConfig ,ILogger<AcumuloController> logger)
        {
            _logger = logger;
            _kafkaConfig = kafkaConfig;
            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                SecurityProtocol = _kafkaConfig.SecurityProtocol,
                SaslMechanism = _kafkaConfig.SaslMechanisms,
                SaslUsername = _kafkaConfig.SaslUsername,
                SaslPassword = _kafkaConfig.SaslPassword
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        /// <summary>
        /// Adiciona um novo registro no banco de dados
        /// </summary>
        /// <param name="acumulo"></param>
        /// <returns></returns>
        [HttpPost("acumuladorIndividual")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Post([FromBody] int acumulo)
        {
            _logger.LogInformation("Produzindo mensagem para o tópico acumulo_pontos");
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = acumulo.ToString()
            };

            await _producer.ProduceAsync("acumulo_pontos", message);
            _logger.LogInformation("mensagem criada com sucesso!");
            return NoContent();
        }

        /// <summary>
        /// Atualiza o valor do acumulador no primeiro registro do banco de dados
        /// </summary>
        /// <param name="acumulo"></param>
        /// <returns></returns>
        [HttpPatch("acumulador")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Patch([FromBody] int acumulo)
        {
            _logger.LogInformation("Produzindo mensagem para o tópico acumulo_pontos_bolsao");
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = acumulo.ToString()
            };

            await _producer.ProduceAsync("acumulo_pontos_bolsao", message);
            _logger.LogInformation("mensagem criada com sucesso!");
            return NoContent();
        }
    }
}