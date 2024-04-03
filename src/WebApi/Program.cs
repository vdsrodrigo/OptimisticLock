using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHealthChecks(builder.Configuration);

var configuration = builder.Configuration;
builder.Services.AddDbContext<AcumuloContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("PostgresDB")));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.MapHealthChecks("/api/health", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.UseHealthChecksUI(delegate(Options options) { options.UIPath = "/healthcheck-ui"; });

app.UseHttpsRedirection();


app.MapPost("/acumuladorIndividual", async (AcumuloContext db, int acumulo) =>
    {
        db.Add(new ShellRepository { Nome = "Shell", Valor = acumulo, RowVersion = 1 });
        await db.SaveChangesAsync();

        return Results.NoContent();
    })
    .WithName("PostAcumuladorIndividualShell")
    .WithOpenApi();

app.MapPatch("/acumulador", async (AcumuloContext db, int acumulo) =>
    {
        const int maximoDeTentativas = 20;
        var totalDeConflitos = 0;
        for (int tentativa = 0; tentativa < maximoDeTentativas; tentativa++)
        {
            try
            {
                var shell = await db.AcumuladorShell.FirstOrDefaultAsync();
                if (shell is null) return Results.NotFound();

                shell.Valor += acumulo;
                shell.RowVersion += 1;

                await db.SaveChangesAsync();
                return Results.Ok($"Quantidade de conflitos ocorridas :{totalDeConflitos}");
            }
            catch (DbUpdateConcurrencyException) when (tentativa < maximoDeTentativas - 1)
            {
                totalDeConflitos++;
            }
        }

        Console.WriteLine(totalDeConflitos);
        return Results.Ok($"Quantidade de conflitos ocorridas :{totalDeConflitos}");
    })
    .WithName("PatchAcumuladorShell")
    .WithOpenApi();

app.Run();