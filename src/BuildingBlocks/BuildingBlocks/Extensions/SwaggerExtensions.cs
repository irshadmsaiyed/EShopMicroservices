using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace BuildingBlocks.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCommonSwagger(this IServiceCollection services, string serviceName)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = serviceName,
                Version = "v1"
            });
        });
        return services;
    }

    public static IApplicationBuilder UseCommonSwagger(this IApplicationBuilder app, string serviceName)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{serviceName} v1");
            c.RoutePrefix = "swagger";
        });
        return app;
    }
}