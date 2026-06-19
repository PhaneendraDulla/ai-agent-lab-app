using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Services.Chat;
using AiAgentLab.Tests.Llm;

namespace AiAgentLab.Tests.Services;

public sealed class ChatServiceTests
{
    private static ChatService BuildService(FakeLLMProvider provider)
    {
        var repo = new FakeConversationRepository();
        var classifier = new NoOpIntentClassifier();
        return new ChatService(provider, repo, classifier);
    }

    [Fact]
    public async Task SendAsync_ReturnsProviderContentAsAnswer()
    {
        var provider = new FakeLLMProvider("RAG means Retrieval-Augmented Generation.");
        var service = BuildService(provider);

        var response = await service.SendAsync(new ChatRequest
        {
            Message = "Explain RAG",
            UserId = 1
        });

        Assert.Equal("RAG means Retrieval-Augmented Generation.", response.Answer);
        Assert.Equal("Fake", response.Provider);
        Assert.Equal("fake-model", response.Model);
    }

    [Fact]
    public async Task SendAsync_PassesUserMessageThroughAsPrompt()
    {
        var provider = new FakeLLMProvider();
        var service = BuildService(provider);

        await service.SendAsync(new ChatRequest { Message = "hello", UserId = 1 });

        Assert.NotNull(provider.LastRequest);
        Assert.Contains("hello", provider.LastRequest!.Prompt);
    }
}