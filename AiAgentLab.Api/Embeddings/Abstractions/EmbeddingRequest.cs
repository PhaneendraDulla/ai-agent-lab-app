namespace AiAgentLab.Api.Embeddings.Abstractions;

/// <summary>Request to embed a single piece of text into a vector.</summary>
public sealed record EmbeddingRequest
{
    public required string Text { get; init; }
}
