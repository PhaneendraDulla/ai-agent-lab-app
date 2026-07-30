namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Selects which vector store backend is active. Bound from the "VectorStore" section.
/// Mirrors <see cref="LlmSettings"/>: controllers/services depend on IVectorStore only,
/// this just tells the factory which implementation to hand out.
/// </summary>
public sealed class VectorStoreSettings
{
    public const string SectionName = "VectorStore";

    /// <summary>Active backend: "Sql" or "Qdrant".</summary>
    public string Provider { get; set; } = "Sql";

    /// <summary>How many top-ranked chunks to retrieve for a RAG query.</summary>
    public int TopK { get; set; } = 4;

    /// <summary>
    /// Minimum cosine similarity (0-1) a chunk must score to be considered relevant.
    /// Chunks below this are dropped rather than injected into the prompt just because
    /// they were the "least bad" of the top-K.
    /// </summary>
    public double MinScore { get; set; } = 0.7;
}
