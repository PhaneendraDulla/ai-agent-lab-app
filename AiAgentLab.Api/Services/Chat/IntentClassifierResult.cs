namespace AiAgentLab.Api.Services.Chat;

/// <summary>Result of intent classification.</summary>
public sealed record IntentClassifierResult
{
    /// <summary>The detected intent domain (e.g., "rag", "embeddings", "debugging").</summary>
    public required string Domain { get; init; }

    /// <summary>The detected intent action (e.g., "explain", "code_example", "troubleshoot").</summary>
    public required string Action { get; init; }

    /// <summary>Confidence score 0.0-1.0 (for future ML models).</summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>Additional metadata about the classification.</summary>
    public Dictionary<string, string>? Metadata { get; init; }
}