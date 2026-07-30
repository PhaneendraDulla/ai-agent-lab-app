using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Embeddings.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Embeddings.Providers;

/// <summary>
/// Embedding provider backed by Gemini's embedContent endpoint. Unlike GeminiLLMProvider,
/// there is no offline fallback here — a RAG pipeline without real embeddings isn't
/// meaningful, so failures are thrown for the caller to handle.
/// </summary>
public sealed class GeminiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiEmbeddingProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GeminiEmbeddingProvider(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "Gemini";

    public async Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured. Set Gemini:ApiKey to enable embeddings.");
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/{_settings.EmbeddingModel}:embedContent";

        var body = new
        {
            content = new
            {
                parts = new object[] { new { text = request.Text } }
            },
            outputDimensionality = _settings.EmbeddingDimensions
        };

        _logger.LogInformation("Requesting embedding from Gemini model {Model}", _settings.EmbeddingModel);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
        httpRequest.Content = JsonContent.Create(body, options: _jsonOptions);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Gemini embedContent request failed with status {(int)httpResponse.StatusCode}. Body: {errorBody}");
        }

        var resp = await httpResponse.Content.ReadFromJsonAsync<GeminiEmbedResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Gemini returned an empty embedContent response body.");

        var values = resp.Embedding?.Values
            ?? throw new InvalidOperationException("Gemini embedContent response did not contain an embedding.");

        return new EmbeddingResponse
        {
            Vector = values,
            Model = _settings.EmbeddingModel,
            Provider = Name
        };
    }

    // --- Gemini wire models (provider-specific) ---
    private sealed record GeminiEmbedResponse
    {
        [JsonPropertyName("embedding")]
        public GeminiEmbedding? Embedding { get; init; }
    }

    private sealed record GeminiEmbedding
    {
        [JsonPropertyName("values")]
        public float[]? Values { get; init; }
    }
}
