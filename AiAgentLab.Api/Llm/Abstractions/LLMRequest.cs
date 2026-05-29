namespace AiAgentLab.Api.Llm.Abstractions;

/// <summary>
/// Provider-agnostic request handed to an <see cref="ILLMProvider"/>.
/// Kept deliberately small for milestone 1; conversation history, tools, and
/// RAG context will be added here in later milestones.
/// </summary>
public sealed record LLMRequest
{
    /// <summary>The user's prompt / message.</summary>
    public required string Prompt { get; init; }
}
