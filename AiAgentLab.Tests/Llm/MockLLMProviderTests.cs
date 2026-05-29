using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Llm.Providers;

namespace AiAgentLab.Tests.Llm;

public sealed class MockLLMProviderTests
{
    [Fact]
    public async Task GenerateAsync_EchoesPromptAndReportsProvider()
    {
        var provider = new MockLLMProvider();

        var response = await provider.GenerateAsync(new LLMRequest { Prompt = "ping" });

        Assert.Equal("Mock", provider.Name);
        Assert.Equal("Mock", response.Provider);
        Assert.Contains("ping", response.Content);
    }
}
