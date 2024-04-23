using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Configs;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/acumulo")]
    public class AcumuloController : ControllerBase
    {
        private readonly AcumuloContext _db;
        private readonly ILogger<AcumuloController> _logger;
        private readonly KafkaConfig _kafkaConfig;
        private IProducer<string, string> _producer;

        public AcumuloController(AcumuloContext db, ILogger<AcumuloController> logger, KafkaConfig kafkaConfig)
        {
            _db = db;
            _logger = logger;
            _kafkaConfig = kafkaConfig;

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
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
            _logger.LogInformation("Adicionando um novo registro no banco de dados");

            _db.Add(new ShellRepository { Nome = "Shell", Valor = acumulo, RowVersion = 1 });
            await _db.SaveChangesAsync();

            // Criar a mensagem
            var message = new Message<string, string>
            {
                Key = null,
                Value = acumulo.ToString()
            };

            // Publicar a mensagem
            var deliveryResult = await _producer.ProduceAsync("acumulo_pontos", message);
            
            
            _logger.LogInformation("Registro adicionado com sucesso");

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
            _logger.LogInformation("Atualizando o valor do acumulador no primeiro registro do banco de dados");
            const int maximoDeTentativas = 20;
            var totalDeConflitos = 0;
            for (int tentativa = 0; tentativa < maximoDeTentativas; tentativa++)
            {
                try
                {
                    var shell = await _db.AcumuladorShell.FirstOrDefaultAsync();
                    if (shell is null) return NotFound();

                    shell.Valor += acumulo;
                    shell.RowVersion += 1;

                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Registro atualizado com sucesso");

                    return Ok($"Quantidade de conflitos ocorridas :{totalDeConflitos}");
                }
                catch (DbUpdateConcurrencyException) when (tentativa < maximoDeTentativas - 1)
                {
                    totalDeConflitos++;
                }
            }

            Console.WriteLine(totalDeConflitos);
            return Ok($"Quantidade de conflitos ocorridas :{totalDeConflitos}");
        }

        private async Task PublishMessage() { }
    }
}