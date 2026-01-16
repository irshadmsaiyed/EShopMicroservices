using BuildingBlocks.Exceptions.Handler;
using Carter;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ordering.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,IConfiguration configuration)
    {
        services.AddCarter();
        Console.Write("###Connection String#####");
        Console.WriteLine(configuration.GetConnectionString("Database"));

        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddHealthChecks()
            .AddSqlServer(configuration.GetConnectionString("Database")!)
            .AddCheck("self", () => HealthCheckResult.Healthy());

        return services;
    }

    public static WebApplication UseApiServices(this WebApplication app)
    {
        app.MapCarter();

        app.UseExceptionHandler(options => { });
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        return app;
    }
}