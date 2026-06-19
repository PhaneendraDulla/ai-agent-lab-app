namespace AiAgentLab.Api.Models.Chat;

/// <summary>
/// Data transfer object representing a single message in a conversation, 
/// including its role (user or assistant), content, timestamp
/// and optional metadata for intent tracking or provider information. 
/// Used within <see cref="ConversationDto"/> to represent conversation history.
/// </summary>
public sealed record MessageDto
{
    /// <summary>A unique identifier for the message.</summary>
    public required string Id { get; init; }
    /// <summary>The ID of the conversation to which this message belongs.</summary>
    public required string ConversationId { get; init; }
    /// <summary>The role of the message sender, e.g. "user" or "assistant".</summary>
    public required string Role { get; init; }  
    /// <summary>The content of the message.</summary>
    public required string Content { get; init; }
    /// <summary>The timestamp when the message was created.</summary>
    public required DateTime CreatedAt { get; init; }
    /// <summary>Optional metadata for intent tracking or provider information.</summary>
    public Dictionary<string, string>? Metadata { get; init; } 
}