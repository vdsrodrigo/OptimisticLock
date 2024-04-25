using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using WebApi.Configs;
using WebApi.Kafka;

namespace WebApi;

public class Startup : IStartup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.ConfigureDb(Configuration);
        services.ConfigureSwagger();
        services.ConfigureHealthChecks(Configuration);

        var kafkaConfig = new KafkaConfig();
        Configuration.GetSection("Kafka").Bind(kafkaConfig);
        services.AddSingleton(kafkaConfig);

        services.AddHostedService<AcumuloConsumer>();

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
        });

        app.UseDeveloperExceptionPage();

        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/api/health", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        });

        app.UseHealthChecksUI(delegate(Options options) { options.UIPath = "/healthcheck-ui"; });
    }
}

public interface IStartup
{
    IConfiguration Configuration { get; }
    void ConfigureServices(IServiceCollection services);
    void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}

public static class StartupExtensions
{
    public static WebApplicationBuilder UseStartup<TStartup>(this WebApplicationBuilder builder) where TStartup : IStartup
    {
        if (Activator.CreateInstance(typeof(TStartup), builder.Configuration) is not IStartup startup)
            throw new ArgumentNullException("Startup not found");

        startup.ConfigureServices(builder.Services);
        var app = builder.Build();
        startup.Configure(app, builder.Environment);
        app.Run();
        return builder;
    }
}