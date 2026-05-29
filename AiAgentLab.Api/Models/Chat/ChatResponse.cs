namespace AiAgentLab.Api.Models.Chat;

/// <summary>Response body for <c>POST /api/chat</c>.</summary>
public sealed record ChatResponse
{
    /// <summary>The model's answer.</summary>
    public required string Answer { get; init; }

    /// <summary>Which provider produced the answer, e.g. "Ollama".</summary>
    public string? Provider { get; init; }

    /// <summary>Which model produced the answer, e.g. "llama3.2".</summary>
    public string? Model { get; init; }
}
