using AiAgentLab.Api.VectorStore.Abstractions;

namespace AiAgentLab.Api.VectorStore.Providers;

/// <summary>
/// Test/DI-wiring fallback that stores nothing and always returns no matches.
/// Mirrors NoOpToolRegistry/NoOpIntentClassifier.
/// </summary>
public sealed class NoOpVectorStore : IVectorStore
{
    public string Name => "NoOp";

    public Task UpsertAsync(VectorChunk chunk, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<ScoredVectorChunk>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ScoredVectorChunk>>(Array.Empty<ScoredVectorChunk>());
}
