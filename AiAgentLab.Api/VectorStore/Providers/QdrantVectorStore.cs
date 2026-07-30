using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.VectorStore.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.VectorStore.Providers;

/// <summary>
/// Vector store backed by a local/remote Qdrant server, talked to over its plain REST
/// API via HttpClient (same pattern as OllamaLLMProvider) rather than the Qdrant.Client
/// NuGet package. The collection is created lazily on first use if it doesn't exist yet.
/// Not wired up to a running Qdrant instance yet — this only needs to compile and be
/// selectable via VectorStore:Provider until Qdrant is installed locally.
/// </summary>
public sealed class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly QdrantSettings _settings;
    private readonly GeminiSettings _geminiSettings;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private bool _collectionEnsured;

    public QdrantVectorStore(
        HttpClient httpClient,
        IOptions<QdrantSettings> settings,
        IOptions<GeminiSettings> geminiSettings,
        ILogger<QdrantVectorStore> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _geminiSettings = geminiSettings.Value;
        _logger = logger;
    }

    public string Name => "Qdrant";

    public async Task UpsertAsync(VectorChunk chunk, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionAsync(cancellationToken);

        var body = new
        {
            points = new object[]
            {
                new
                {
                    id = chunk.Id,
                    vector = chunk.Embedding,
                    payload = new
                    {
                        documentName = chunk.DocumentName,
                        chunkIndex = chunk.ChunkIndex,
                        text = chunk.Text
                    }
                }
            }
        };

        using var response = await _httpClient.PutAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points", body, _jsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Qdrant upsert failed with status {(int)response.StatusCode}. Body: {errorBody}");
        }
    }

    public async Task DeleteByDocumentNameAsync(string documentName, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionAsync(cancellationToken);

        var body = new
        {
            filter = new
            {
                must = new object[]
                {
                    new { key = "documentName", match = new { value = documentName } }
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points/delete", body, _jsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Qdrant delete-by-document failed for '{Document}' with status {StatusCode}. Body: {Body}",
                documentName, (int)response.StatusCode, errorBody);
        }
        else
        {
            _logger.LogInformation(
                "QdrantVectorStore: deleted existing chunks for document '{Document}' before re-ingestion.",
                documentName);
        }
    }

    public async Task<IReadOnlyList<ScoredVectorChunk>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        await EnsureCollectionAsync(cancellationToken);

        var body = new
        {
            vector = queryVector,
            limit = topK,
            with_payload = true
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"/collections/{_settings.CollectionName}/points/search", body, _jsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Qdrant search failed with status {(int)response.StatusCode}. Body: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Qdrant returned an empty search response body.");

        return (result.Result ?? Array.Empty<QdrantScoredPoint>())
            .Select(point => new ScoredVectorChunk
            {
                Chunk = new VectorChunk
                {
                    Id = ExtractPointId(point.Id),
                    DocumentName = point.Payload?.DocumentName ?? string.Empty,
                    ChunkIndex = point.Payload?.ChunkIndex ?? 0,
                    Text = point.Payload?.Text ?? string.Empty,
                    Embedding = queryVector // Qdrant doesn't echo vectors back unless requested; not needed by callers.
                },
                Score = point.Score
            })
            .ToList();
    }

    // Creates the collection on first use if it doesn't already exist. Cheap in-memory
    // flag avoids a round-trip on every call once we know it's there.
    private async Task EnsureCollectionAsync(CancellationToken cancellationToken)
    {
        if (_collectionEnsured)
            return;

        var checkResponse = await _httpClient.GetAsync($"/collections/{_settings.CollectionName}", cancellationToken);
        if (checkResponse.StatusCode == HttpStatusCode.OK)
        {
            _collectionEnsured = true;
            return;
        }

        _logger.LogInformation("Qdrant collection '{Collection}' not found; creating it.", _settings.CollectionName);

        var createBody = new
        {
            vectors = new
            {
                size = _geminiSettings.EmbeddingDimensions,
                distance = "Cosine"
            }
        };

        using var createResponse = await _httpClient.PutAsJsonAsync(
            $"/collections/{_settings.CollectionName}", createBody, _jsonOptions, cancellationToken);

        if (!createResponse.IsSuccessStatusCode)
        {
            var errorBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Qdrant collection creation failed with status {(int)createResponse.StatusCode}. Body: {errorBody}");
        }

        _collectionEnsured = true;
    }

    // Qdrant point IDs can be returned as a JSON string or number depending on how they
    // were stored; normalize either shape to a plain string for our VectorChunk model.
    private static string ExtractPointId(JsonElement? id)
    {
        if (id is null || id.Value.ValueKind == JsonValueKind.Undefined || id.Value.ValueKind == JsonValueKind.Null)
            return string.Empty;

        return id.Value.ValueKind == JsonValueKind.String
            ? id.Value.GetString() ?? string.Empty
            : id.Value.GetRawText();
    }

    // --- Qdrant wire models (provider-specific) ---
    private sealed record QdrantSearchResponse
    {
        [JsonPropertyName("result")]
        public QdrantScoredPoint[]? Result { get; init; }
    }

    private sealed record QdrantScoredPoint
    {
        [JsonPropertyName("id")]
        public JsonElement? Id { get; init; }

        [JsonPropertyName("score")]
        public double Score { get; init; }

        [JsonPropertyName("payload")]
        public QdrantPayload? Payload { get; init; }
    }

    private sealed record QdrantPayload
    {
        [JsonPropertyName("documentName")]
        public string? DocumentName { get; init; }

        [JsonPropertyName("chunkIndex")]
        public int? ChunkIndex { get; init; }

        [JsonPropertyName("text")]
        public string? Text { get; init; }
    }
}
