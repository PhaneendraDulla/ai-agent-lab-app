namespace AiAgentLab.Api.VectorStore.Abstractions;

/// <summary>
/// The single seam every vector store backend implements. Services depend on this
/// abstraction only — never on a concrete store. Adding a new backend (Pinecone,
/// Weaviate, ...) means adding an implementation and changing registration/configuration.
/// </summary>
public interface IVectorStore
{
    /// <summary>Stable backend name, e.g. "Sql" or "Qdrant".</summary>
    string Name { get; }

    /// <summary>Insert or update a chunk's vector and text.</summary>
    Task UpsertAsync(VectorChunk chunk, CancellationToken cancellationToken = default);

    /// <summary>Find the topK chunks most similar to queryVector, ranked by cosine similarity (highest first).</summary>
    Task<IReadOnlyList<ScoredVectorChunk>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default);
}
