namespace AiAgentLab.Api.Services.Documents;

/// <summary>Reads local documents, chunks and embeds them, and upserts them into the vector store.</summary>
public interface IDocumentIngestionService
{
    Task<DocumentIngestionResult> IngestAsync(CancellationToken cancellationToken = default);
}
