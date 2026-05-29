namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Configuration for the local Ollama server. Bound from the "Ollama" section.
/// </summary>
public sealed class OllamaSettings
{
    public const string SectionName = "Ollama";

    /// <summary>Base URL of the local Ollama server.</summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>Model name to use for chat, e.g. "llama3.2".</summary>
    public string Model { get; set; } = "llama3.2";

    /// <summary>Request timeout in seconds for Ollama calls.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
