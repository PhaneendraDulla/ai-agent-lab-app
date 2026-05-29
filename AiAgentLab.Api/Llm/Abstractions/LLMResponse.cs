namespace AiAgentLab.Api.Llm.Abstractions;

/// <summary>
/// Provider-agnostic response returned from an <see cref="ILLMProvider"/>.
/// </summary>
public sealed record LLMResponse
{
    /// <summary>The generated text.</summary>
    public required string Content { get; init; }

    /// <summary>Model that produced the response, e.g. "llama3.2".</summary>
    public string? Model { get; init; }

    /// <summary>Name of the provider that produced the response, e.g. "Ollama".</summary>
    public string? Provider { get; init; }
}
