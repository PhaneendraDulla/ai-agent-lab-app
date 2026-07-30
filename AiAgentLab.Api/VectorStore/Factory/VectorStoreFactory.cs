using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.VectorStore.Abstractions;
using AiAgentLab.Api.VectorStore.Providers;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.VectorStore.Factory;

/// <summary>
/// Picks a vector store implementation by name from <see cref="VectorStoreSettings.Provider"/>.
/// Concrete stores are resolved from DI so their own dependencies (DbContext, HttpClient,
/// options, loggers) are wired correctly. Defaults to Sql so the app works with zero
/// extra infrastructure out of the box.
/// </summary>
public sealed class VectorStoreFactory : IVectorStoreFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly VectorStoreSettings _settings;

    public VectorStoreFactory(IServiceProvider serviceProvider, IOptions<VectorStoreSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public IVectorStore Create()
    {
        return _settings.Provider.Trim().ToLowerInvariant() switch
        {
            "sql" => _serviceProvider.GetRequiredService<SqlVectorStore>(),
            "qdrant" => _serviceProvider.GetRequiredService<QdrantVectorStore>(),
            _ => throw new InvalidOperationException(
                $"Unknown vector store provider '{_settings.Provider}'. Valid values: Sql, Qdrant.")
        };
    }
}
