namespace AiAgentLab.Api.Services.Documents;

/// <summary>Summary of a document ingestion run, returned to the caller.</summary>
public sealed record DocumentIngestionResult
{
    public required int DocumentsProcessed { get; init; }
    public required int ChunksIngested { get; init; }
    public required IReadOnlyList<string> DocumentNames { get; init; }
}
