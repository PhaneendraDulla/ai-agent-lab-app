using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Llm.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Llm.Providers;

/// <summary>
/// Talks to a local Ollama server using its <c>/api/chat</c> endpoint.
/// The Ollama-specific wire models live in this file only, so the rest of the app
/// never sees provider-specific request/response shapes.
/// </summary>
public sealed class OllamaLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaLLMProvider> _logger;

    public OllamaLLMProvider(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaLLMProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "Ollama";

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var ollamaRequest = new OllamaChatRequest
        {
            Model = _settings.Model,
            Stream = false,
            Messages = [new OllamaMessage { Role = "user", Content = request.Prompt }]
        };

        _logger.LogInformation("Sending chat request to Ollama model {Model}", _settings.Model);

        using var httpResponse = await _httpClient.PostAsJsonAsync("/api/chat", ollamaRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var ollamaResponse = await httpResponse.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response body.");

        return new LLMResponse
        {
            Content = ollamaResponse.Message?.Content ?? string.Empty,
            Model = ollamaResponse.Model ?? _settings.Model,
            Provider = Name
        };
    }

    // --- Ollama wire models (provider-specific, kept private to this file) ---

    private sealed record OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("messages")]
        public required IReadOnlyList<OllamaMessage> Messages { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }
    }

    private sealed record OllamaMessage
    {
        [JsonPropertyName("role")]
        public required string Role { get; init; }

        [JsonPropertyName("content")]
        public required string Content { get; init; }
    }

    private sealed record OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; init; }
    }
}
