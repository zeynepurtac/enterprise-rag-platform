using Microsoft.EntityFrameworkCore;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Infrastructure.Persistence;

namespace RagPlatform.Api.Infrastructure;

/// <summary>
/// Ensures the database schema and the Qdrant collection exist before the API
/// starts serving traffic. Retries so the container can start in parallel with
/// Postgres / Qdrant without a strict boot order.
/// </summary>
public static class StartupInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("StartupInitializer");

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await RetryAsync(logger, "database", async () =>
        {
            await db.Database.EnsureCreatedAsync();
        });

        var vectorSearch = scope.ServiceProvider.GetRequiredService<IVectorSearchService>();
        await RetryAsync(logger, "vector store", async () =>
        {
            await vectorSearch.EnsureCollectionAsync();
        });
    }

    private static async Task RetryAsync(
        ILogger logger,
        string name,
        Func<Task> action,
        int attempts = 30,
        int delaySeconds = 3)
    {
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await action();
                logger.LogInformation("Initialised {Component}.", name);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Waiting for {Component} (attempt {Attempt}/{Max}): {Message}",
                    name, attempt, attempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }

        logger.LogError("Component {Component} was not ready after {Max} attempts.", name, attempts);
    }
}
