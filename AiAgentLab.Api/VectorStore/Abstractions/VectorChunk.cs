namespace AiAgentLab.Api.VectorStore.Abstractions;

/// <summary>A single embedded chunk of a source document, as stored in the vector store.</summary>
public sealed record VectorChunk
{
    public required string Id { get; init; }
    public required string DocumentName { get; init; }
    public required int ChunkIndex { get; init; }
    public required string Text { get; init; }
    public required float[] Embedding { get; init; }
}

/// <summary>A chunk returned from a similarity search, along with its score.</summary>
public sealed record ScoredVectorChunk
{
    public required VectorChunk Chunk { get; init; }
    public required double Score { get; init; }
}
