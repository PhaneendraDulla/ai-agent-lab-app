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
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent";

        var body = new GeminiRequest
        {
            Contents =
            [
                new GeminiContent
            {
                Parts = [new GeminiPart { Text = request.Prompt }]
            }
            ]
        };

        _logger.LogInformation("Gemini request URL: {Url}", url);
        _logger.LogInformation("Sending generateContent request to Gemini model {Model}", _settings.Model);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(body);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var resp = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Gemini returned an empty response body.");

        var text = resp.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
            ?? string.Empty;

        return new LLMResponse
        {
            Content = text,
            Model = _settings.Model,
            Provider = Name
        };
    }

    // --- Gemini wire models (provider-specific, kept private to this file) ---

    private sealed record GeminiRequest
    {
        [JsonPropertyName("contents")]
        public required IReadOnlyList<GeminiContent> Contents { get; init; }
    }

    private sealed record GeminiContent
    {
        [JsonPropertyName("parts")]
        public required IReadOnlyList<GeminiPart> Parts { get; init; }
    }

    private sealed record GeminiPart
    {
        [JsonPropertyName("text")]
        public required string Text { get; init; }
    }

    private sealed record GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; init; }
    }

    private sealed record GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; init; }
    }

    
}
