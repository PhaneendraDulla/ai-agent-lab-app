namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Top-level LLM configuration. Bound from the "Llm" section of appsettings.
/// <see cref="Provider"/> selects which <c>ILLMProvider</c> implementation runs at runtime.
/// </summary>
public sealed class LlmSettings
{
    public const string SectionName = "Llm";

    /// <summary>
    /// Which provider to use, e.g. "Ollama" or "Mock". Matched case-insensitively
    /// by the provider factory.
    /// </summary>
    public string Provider { get; set; } = "Gemini";
}
