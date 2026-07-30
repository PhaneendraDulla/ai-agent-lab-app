using AiAgentLab.Api.Embeddings.Abstractions;

namespace AiAgentLab.Api.Embeddings.Providers;

/// <summary>
/// Test/DI-wiring fallback that returns an empty vector instead of calling a real
/// embedding backend. Mirrors NoOpToolRegistry/NoOpIntentClassifier.
/// </summary>
public sealed class NoOpEmbeddingProvider : IEmbeddingProvider
{
    public string Name => "NoOp";

    public Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EmbeddingResponse
        {
            Vector = Array.Empty<float>(),
            Model = "none",
            Provider = Name
        });
    }
}
