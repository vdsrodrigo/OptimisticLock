using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebApi.Configs;

public static class HealthCheck
{
    public static void ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddRedis(configuration.GetConnectionString("Redis")!, name: "Redis", tags: new[] { "cache" })
            .AddNpgSql(configuration.GetConnectionString("PostgresDB")!, name: "PostgresDB", tags: new[] { "database" })
            .AddMongoDb(configuration.GetConnectionString("MongoDB")!, "MongoDB", HealthStatus.Unhealthy, new[] { "database" });
        
        services.AddHealthChecksUI(opt =>
            {
                opt.SetEvaluationTimeInSeconds(10); //time in seconds between check    
                opt.MaximumHistoryEntriesPerEndpoint(60); //maximum history of checks    
                opt.SetApiMaxActiveRequests(1); //api requests concurrency    
                opt.AddHealthCheckEndpoint("feedback api", "/api/health"); //map health check api    

            })
            .AddInMemoryStorage();
    }
}