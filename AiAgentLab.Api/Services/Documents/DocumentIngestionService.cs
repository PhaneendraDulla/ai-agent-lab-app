using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Embeddings.Abstractions;
using AiAgentLab.Api.VectorStore.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Services.Documents;

/// <summary>
/// Reads .txt/.md files from the configured folder, chunks each one, embeds every
/// chunk, and upserts it into the active vector store. Re-running ingestion re-embeds
/// and overwrites existing chunks for the same document (deterministic chunk IDs),
/// so it's safe to call again after editing a document.
/// </summary>
public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly DocumentChunker _chunker;
    private readonly DocumentIngestionSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentIngestionService> _logger;

    private static readonly string[] SupportedExtensions = { ".txt", ".md" };

    public DocumentIngestionService(
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        DocumentChunker chunker,
        IOptions<DocumentIngestionSettings> settings,
        IWebHostEnvironment environment,
        ILogger<DocumentIngestionService> logger)
    {
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _chunker = chunker;
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<DocumentIngestionResult> IngestAsync(CancellationToken cancellationToken = default)
    {
        var folderPath = Path.IsPathRooted(_settings.FolderPath)
            ? _settings.FolderPath
            : Path.Combine(_environment.ContentRootPath, _settings.FolderPath);

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("DocumentIngestion: folder '{Folder}' does not exist. Nothing to ingest.", folderPath);
            return new DocumentIngestionResult { DocumentsProcessed = 0, ChunksIngested = 0, DocumentNames = Array.Empty<string>() };
        }

        var files = Directory.GetFiles(folderPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToList();

        var documentNames = new List<string>();
        var totalChunks = 0;

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var documentName = Path.GetFileName(filePath);
            var text = await File.ReadAllTextAsync(filePath, cancellationToken);
            var chunks = await _chunker.ChunkAsync(text, cancellationToken);

            await _vectorStore.DeleteByDocumentNameAsync(documentName, cancellationToken);

            _logger.LogInformation("DocumentIngestion: '{Document}' split into {ChunkCount} chunk(s).", documentName, chunks.Count);

            for (var i = 0; i < chunks.Count; i++)
            {
                var embedding = await _embeddingProvider.EmbedAsync(new EmbeddingRequest { Text = chunks[i] }, cancellationToken);

                await _vectorStore.UpsertAsync(new VectorChunk
                {
                    Id = BuildChunkId(documentName, i),
                    DocumentName = documentName,
                    ChunkIndex = i,
                    Text = chunks[i],
                    Embedding = embedding.Vector
                }, cancellationToken);

                totalChunks++;
            }

            documentNames.Add(documentName);
        }

        _logger.LogInformation(
            "DocumentIngestion: processed {DocCount} document(s), ingested {ChunkCount} chunk(s).",
            documentNames.Count, totalChunks);

        return new DocumentIngestionResult
        {
            DocumentsProcessed = documentNames.Count,
            ChunksIngested = totalChunks,
            DocumentNames = documentNames
        };
    }

    // Deterministic per document+index so re-ingesting a document overwrites its old
    // chunks instead of accumulating duplicates.
    private static string BuildChunkId(string documentName, int chunkIndex) => $"{documentName}::{chunkIndex}";
}
