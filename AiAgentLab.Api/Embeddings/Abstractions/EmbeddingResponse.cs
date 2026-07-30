namespace AiAgentLab.Api.Embeddings.Abstractions;

/// <summary>Result of embedding a piece of text.</summary>
public sealed record EmbeddingResponse
{
    public required float[] Vector { get; init; }
    public required string Model { get; init; }
    public required string Provider { get; init; }
}
