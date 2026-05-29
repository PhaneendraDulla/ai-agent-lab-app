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
}
