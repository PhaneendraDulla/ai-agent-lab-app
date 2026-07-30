namespace AiAgentLab.Api.Data.Entities;

/// <summary>
/// A single embedded chunk of a source document. Embedding is stored as a JSON array
/// of floats — simple and readable, meant to teach the mechanics rather than be
/// optimized. Similarity search is brute-force cosine similarity computed in C#.
/// </summary>
public class VectorChunkEntity
{
    public string Id { get; set; } = null!;
    public string DocumentName { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; } = null!;
    // JSON-serialized float[], e.g. "[0.123,0.456,...]"
    public string Embedding { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
