using Microsoft.OpenApi.Models;

namespace Automation.Api.Infrastructure;

public static class SwaggerExtensions
{
    public static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Automation.Api", Version = "v1" });
        });
    }

    public static void UseSwagger(this IApplicationBuilder app, IConfiguration configuration)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Automation.Api v1"));
    }
}