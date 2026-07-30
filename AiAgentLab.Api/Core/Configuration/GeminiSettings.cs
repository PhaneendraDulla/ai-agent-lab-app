namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Configuration for a Gemini (Generative Language) provider.
/// Bound from the "Gemini" section.
/// </summary>
public sealed class GeminiSettings
{
    public const string SectionName = "Gemini";

    /// <summary>Base URL for the Generative Language API.</summary>
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    /// <summary>API key for authenticating requests (use user-secrets or env in production).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model resource name, e.g. "models/text-bison-001".</summary>
    public string Model { get; set; } = "models/text-bison-001";

    /// <summary>Request timeout in seconds for Gemini calls.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Embedding model resource name, e.g. "models/text-embedding-004".</summary>
    public string EmbeddingModel { get; set; } = "models/gemini-embedding-001";

    /// <summary>Output vector size for the embedding model (768 for text-embedding-004).</summary>
    public int EmbeddingDimensions { get; set; } = 768;

    /// <summary>
    /// Sampling temperature for chat generation (0-2). Kept low so answers stay grounded
    /// in retrieved RAG context and tool results rather than drifting into creative phrasing.
    /// </summary>
    public double Temperature { get; set; } = 0.2;
}
