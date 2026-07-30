namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Configuration for local document ingestion into the vector store.
/// Bound from the "DocumentIngestion" section.
/// </summary>
public sealed class DocumentIngestionSettings
{
    public const string SectionName = "DocumentIngestion";

    /// <summary>Folder to scan for .txt/.md files. Relative paths resolve against the app's content root.</summary>
    public string FolderPath { get; set; } = "Documents";

    /// <summary>Target chunk size in tokens.</summary>
    public int ChunkSizeTokens { get; set; } = 500;

    /// <summary>Token overlap between consecutive chunks, so context isn't lost at chunk boundaries.</summary>
    public int ChunkOverlapTokens { get; set; } = 50;
}
