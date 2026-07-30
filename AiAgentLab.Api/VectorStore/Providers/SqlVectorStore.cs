using System.Text.Json;
using AiAgentLab.Api.Data;
using AiAgentLab.Api.Data.Entities;
using AiAgentLab.Api.VectorStore.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AiAgentLab.Api.VectorStore.Providers;

/// <summary>
/// SQL-backed vector store. Embeddings are stored as a JSON array of floats and
/// similarity is computed brute-force in C# at query time — simple and readable,
/// meant to teach the mechanics rather than be optimized. Fine for a learning-scale
/// document set; a real deployment would push this into a dedicated vector database.
/// </summary>
public sealed class SqlVectorStore : IVectorStore
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<SqlVectorStore> _logger;

    public SqlVectorStore(AppDbContext ctx, ILogger<SqlVectorStore> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public string Name => "Sql";

    public async Task UpsertAsync(VectorChunk chunk, CancellationToken cancellationToken = default)
    {
        var existing = await _ctx.VectorChunks.FindAsync(new object[] { chunk.Id }, cancellationToken);
        var embeddingJson = JsonSerializer.Serialize(chunk.Embedding);

        if (existing == null)
        {
            _ctx.VectorChunks.Add(new VectorChunkEntity
            {
                Id = chunk.Id,
                DocumentName = chunk.DocumentName,
                ChunkIndex = chunk.ChunkIndex,
                ChunkText = chunk.Text,
                Embedding = embeddingJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.DocumentName = chunk.DocumentName;
            existing.ChunkIndex = chunk.ChunkIndex;
            existing.ChunkText = chunk.Text;
            existing.Embedding = embeddingJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoredVectorChunk>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        var allChunks = await _ctx.VectorChunks.ToListAsync(cancellationToken);

        var scored = allChunks
            .Select(entity =>
            {
                var embedding = JsonSerializer.Deserialize<float[]>(entity.Embedding) ?? Array.Empty<float>();
                var score = CosineSimilarity(queryVector, embedding);
                return new ScoredVectorChunk
                {
                    Chunk = new VectorChunk
                    {
                        Id = entity.Id,
                        DocumentName = entity.DocumentName,
                        ChunkIndex = entity.ChunkIndex,
                        Text = entity.ChunkText,
                        Embedding = embedding
                    },
                    Score = score
                };
            })
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .ToList();

        _logger.LogInformation(
            "SqlVectorStore: searched {Total} chunk(s), returning top {TopK}.", allChunks.Count, scored.Count);

        return scored;
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0.0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
            return 0.0;

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
