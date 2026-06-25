using AiAgentLab.Api.Llm.Abstractions;
using System.Linq;

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
        // Build a simple text response from the incoming messages for deterministic behavior in tests
        var joined = string.Join(" | ", request.Messages.Select(m => $"{m.Role}: {m.Content}"));
        var response = new LLMResponse
        {
            Text = $"[mock] You said: {joined}",
            Model = "mock",
            Provider = Name
        };

        return Task.FromResult(response);
    }
}
