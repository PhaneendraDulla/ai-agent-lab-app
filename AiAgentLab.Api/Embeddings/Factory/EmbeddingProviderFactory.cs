using AiAgentLab.Api.Embeddings.Abstractions;
using AiAgentLab.Api.Embeddings.Providers;

namespace AiAgentLab.Api.Embeddings.Factory;

/// <summary>
/// Only Gemini is implemented today, but the factory seam is here so adding
/// another embedding backend later doesn't require touching any caller.
/// </summary>
public sealed class EmbeddingProviderFactory : IEmbeddingProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public EmbeddingProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEmbeddingProvider Create()
    {
        return _serviceProvider.GetRequiredService<GeminiEmbeddingProvider>();
    }
}
