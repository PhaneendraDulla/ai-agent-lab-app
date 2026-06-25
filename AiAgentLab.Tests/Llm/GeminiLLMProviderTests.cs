using System.Net;
using System.Text;
using System.Text.Json;
using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Llm.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Tests.Llm;

public sealed class GeminiLLMProviderTests
{
    [Fact]
    public async Task GenerateAsync_SendsPlainPromptAndParsesStringArguments()
    {
        var handler = new CapturingHttpMessageHandler();
        var client = new HttpClient(handler);
        var provider = new GeminiLLMProvider(
            client,
            Options.Create(new GeminiSettings { ApiKey = "test-key", Model = "gemini-2.5-flash" }),
            NullLogger<GeminiLLMProvider>.Instance);

        var response = await provider.GenerateAsync(new LLMRequest
        {
            Messages = new[]
            {
                new LLMMessage { Role = "user", Content = "what's today's date" }
            }
        });

        Assert.NotNull(response.ToolCall);
        Assert.Equal("get_stock_price", response.ToolCall!.Name);
        Assert.Equal("MSFT", response.ToolCall.Args.GetProperty("symbol").GetString());
        Assert.Contains("User:", handler.BodyText);
        Assert.DoesNotContain("\"role\"", handler.BodyText);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsLocalFallbackWhenApiKeyIsMissing()
    {
        var provider = new GeminiLLMProvider(
            new HttpClient(new StubHttpMessageHandler()),
            Options.Create(new GeminiSettings { ApiKey = "", Model = "gemini-2.5-flash" }),
            NullLogger<GeminiLLMProvider>.Instance);

        var response = await provider.GenerateAsync(new LLMRequest
        {
            Messages = new[]
            {
                new LLMMessage { Role = "user", Content = "what is today's date" }
            }
        });

        Assert.NotNull(response.ToolCall);
        Assert.Equal("get_current_date", response.ToolCall!.Name);
        Assert.Empty(response.ToolCall.Args.EnumerateObject());
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        public string BodyText { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            BodyText = await request.Content!.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"candidates\":[{\"content\":{\"parts\":[{\"functionCall\":{\"name\":\"get_stock_price\",\"arguments\":\"{\\\"symbol\\\":\\\"MSFT\\\"}\"}}]}}]}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }
    }
}
