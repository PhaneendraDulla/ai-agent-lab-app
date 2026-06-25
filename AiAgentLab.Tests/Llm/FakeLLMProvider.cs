using AiAgentLab.Api.Llm.Abstractions;

namespace AiAgentLab.Tests.Llm;

/// <summary>
/// Test double for <see cref="ILLMProvider"/>. Records the last request and
/// returns a canned response, so service tests never touch a real model.
/// </summary>
public sealed class FakeLLMProvider : ILLMProvider
{
    private readonly string _cannedContent;

    public FakeLLMProvider(string cannedContent = "fake answer")
    {
        _cannedContent = cannedContent;
    }

    public string Name => "Fake";

    public LLMRequest? LastRequest { get; private set; }

    public Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;

        var response = new LLMResponse
        {
            Text = _cannedContent,
            Model = "fake-model",
            Provider = Name
        };

        return Task.FromResult(response);
    }
}
