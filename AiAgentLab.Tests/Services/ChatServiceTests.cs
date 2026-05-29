using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Services.Chat;
using AiAgentLab.Tests.Llm;

namespace AiAgentLab.Tests.Services;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task SendAsync_ReturnsProviderContentAsAnswer()
    {
        var provider = new FakeLLMProvider("RAG means Retrieval-Augmented Generation.");
        var service = new ChatService(provider);

        var response = await service.SendAsync(new ChatRequest { Message = "Explain RAG" });

        Assert.Equal("RAG means Retrieval-Augmented Generation.", response.Answer);
        Assert.Equal("Fake", response.Provider);
        Assert.Equal("fake-model", response.Model);
    }

    [Fact]
    public async Task SendAsync_PassesUserMessageThroughAsPrompt()
    {
        var provider = new FakeLLMProvider();
        var service = new ChatService(provider);

        await service.SendAsync(new ChatRequest { Message = "hello" });

        Assert.NotNull(provider.LastRequest);
        Assert.Equal("hello", provider.LastRequest!.Prompt);
    }
}
