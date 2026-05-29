using AiAgentLab.Api.Llm.Abstractions;

namespace AiAgentLab.Api.Llm.Providers;

/// <summary>
/// Deterministic provider used for unit tests and as a no-dependency fallback.
/// Never calls a real model, so it is safe in CI and offline.
/// </summary>
public sealed class MockLLMProvider : ILLMProvider
{
    public string Name => "Mock";

    public Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var response = new LLMResponse
        {
            Content = $"[mock] You said: {request.Prompt}",
            Model = "mock",
            Provider = Name
        };

        return Task.FromResult(response);
    }
}
