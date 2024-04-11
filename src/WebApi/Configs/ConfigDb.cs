using Microsoft.EntityFrameworkCore;

namespace WebApi.Configs;

public static class ConfigDb
{
    public static void ConfigureDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AcumuloContext>(options =>
             options.UseNpgsql(configuration.GetConnectionString("PostgresDB")));
    }
}