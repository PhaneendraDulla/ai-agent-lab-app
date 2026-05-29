using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Llm.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Llm.Providers;

/// <summary>
/// Provider implementation that calls Google's Generative Language API (Gemini)
/// using an API key. The wire request/response shapes are kept private to this file.
/// </summary>
public sealed class GeminiLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiLLMProvider> _logger;

    public GeminiLLMProvider(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiLLMProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "Gemini";

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"/v1/models/{_settings.Model}:generate?key={_settings.ApiKey}";

        var body = new GeminiGenerateRequest
        {
            Prompt = new GeminiPrompt { Text = request.Prompt }
        };

        _logger.LogInformation("Sending generate request to Gemini model {Model}", _settings.Model);

        using var httpResponse = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var resp = await httpResponse.Content.ReadFromJsonAsync<GeminiGenerateResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Gemini returned an empty response body.");

        var content = resp.Candidates?.FirstOrDefault()?.Content ?? string.Empty;

        return new LLMResponse
        {
            Content = content,
            Model = _settings.Model,
            Provider = Name
        };
    }

    private sealed record GeminiGenerateRequest
    {
        [JsonPropertyName("prompt")]
        public GeminiPrompt Prompt { get; init; } = null!;
    }

    private sealed record GeminiPrompt
    {
        [JsonPropertyName("text")]
        public required string Text { get; init; }
    }

    private sealed record GeminiGenerateResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; init; }
    }

    private sealed record GeminiCandidate
    {
        [JsonPropertyName("content")]
        public string? Content { get; init; }
    }
}
