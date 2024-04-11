using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/acumulo")]
    public class AcumuloController : ControllerBase
    {

        private readonly AcumuloContext _db;
        public AcumuloController(AcumuloContext db)
        {
            _db = db;
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

            _db.Add(new ShellRepository { Nome = "Shell", Valor = acumulo, RowVersion = 1 });
            await _db.SaveChangesAsync();

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
    }
}