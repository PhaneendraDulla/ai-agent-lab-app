namespace AiAgentLab.Api.Llm.Abstractions;

/// <summary>
/// The single seam every LLM backend implements. Controllers and services depend
/// on this abstraction only — never on a concrete provider. Adding OpenAI, Claude,
/// Azure OpenAI, or Gemini later means adding a new implementation and changing
/// registration/configuration, nothing else.
/// </summary>
public interface ILLMProvider
{
    /// <summary>Stable provider name, e.g. "Ollama" or "Mock".</summary>
    string Name { get; }

    /// <summary>Generate a completion for the given request.</summary>
    Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default);
}
