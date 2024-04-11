using Microsoft.OpenApi.Models;

namespace WebApi.Configs;

public static class ConfigSwagger
{
    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "VdsTech Poc API",
                Description = "Api para testar design de código, patterns, idéias, ferramentas e tecnologias!"
            });
        });
    }
}