using System.Net;
using System.Net.Http.Json;
using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Tests.Controllers;

public sealed class ChatControllerTests : IClassFixture<MockProviderWebApplicationFactory>
{
    private readonly MockProviderWebApplicationFactory _factory;

    public ChatControllerTests(MockProviderWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostChat_ReturnsAnswerFromMockProvider()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest
        {
            Message = "hello",
            UserId = 1
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(body);
        Assert.Equal("Mock", body!.Provider);
        Assert.Contains("hello", body.Answer);
    }

    [Fact]
    public async Task PostChat_WithMissingMessage_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/chat", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
