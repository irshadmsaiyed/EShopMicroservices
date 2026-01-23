using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Discount.Grpc.Data;

public static class Extensions
{
    public static IApplicationBuilder UseMigration(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DiscountContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DiscountContext>>();
        
        var retry = Policy.Handle<SqliteException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retry, _) =>
                {
                    logger.LogWarning(exception,
                        "Retry {Retry} while migrating database. Waiting {Delay}s",
                        retry, timeSpan.TotalSeconds);
                }
            );

        retry.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Migrated database completely.");
        });
        return app;
    }
}