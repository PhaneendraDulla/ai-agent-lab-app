namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Configuration for a local/remote Qdrant vector database, talked to over its REST API.
/// Bound from the "Qdrant" section. Not required unless VectorStore:Provider is "Qdrant".
/// </summary>
public sealed class QdrantSettings
{
    public const string SectionName = "Qdrant";

    /// <summary>Base URL for the Qdrant REST API.</summary>
    public string BaseUrl { get; set; } = "http://localhost:6333";

    /// <summary>Collection name used to store document chunk vectors.</summary>
    public string CollectionName { get; set; } = "document_chunks";

    /// <summary>Request timeout in seconds for Qdrant calls.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
