using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RagPlatform.Application.Common.Interfaces;
using RagPlatform.Infrastructure.BackgroundTasks;
using RagPlatform.Infrastructure.Documents;
using RagPlatform.Infrastructure.Embeddings;
using RagPlatform.Infrastructure.Llm;
using RagPlatform.Infrastructure.Options;
using RagPlatform.Infrastructure.Persistence;
using RagPlatform.Infrastructure.Rag;
using RagPlatform.Infrastructure.Vector;

namespace RagPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));
        services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        AddPersistence(services, configuration);
        AddHttpClients(services, configuration);

        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IRagService, RagService>();

        services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(capacity: 100));
        services.AddHostedService<QueuedHostedService>();

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
                        ?? new DatabaseOptions();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (string.Equals(dbOptions.Provider, "sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(dbOptions.ConnectionString);
            }
            else
            {
                options.UseNpgsql(dbOptions.ConnectionString);
            }
        });

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());
    }

    private static void AddHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        var ai = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        var qdrant = configuration.GetSection(QdrantOptions.SectionName).Get<QdrantOptions>()
                     ?? new QdrantOptions();

        var aiBaseUrl = EnsureTrailingSlash(ai.BaseUrl);
        var qdrantBaseUrl = EnsureTrailingSlash(qdrant.BaseUrl);

        void ConfigureAiClient(HttpClient client)
        {
            client.BaseAddress = new Uri(aiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(ai.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(ai.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", ai.ApiKey);
            }
        }

        services.AddHttpClient<IEmbeddingService, OpenAiCompatibleEmbeddingService>(ConfigureAiClient);
        services.AddHttpClient<IChatCompletionService, OpenAiCompatibleChatCompletionService>(ConfigureAiClient);

        services.AddHttpClient<IVectorSearchService, QdrantVectorSearchService>(client =>
        {
            client.BaseAddress = new Uri(qdrantBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
            if (!string.IsNullOrWhiteSpace(qdrant.ApiKey))
            {
                client.DefaultRequestHeaders.Add("api-key", qdrant.ApiKey);
            }
        });
    }

    private static string EnsureTrailingSlash(string url)
        => url.EndsWith('/') ? url : url + "/";
}
