namespace AiAgentLab.Api.Models.Chat;

/// <summary>
/// Data transfer object representing a conversation, including its metadata and messages. 
/// Used for returning conversation history in endpoints like <c>GET /api/conversations/{id}</c>.
/// </summary>
public sealed record ConversationDto
{
    /// <summary>A unique identifier for the conversation.</summary>
    public required string Id { get; init; }
    /// <summary>The ID of the user who owns this conversation.</summary>
    public required string UserId { get; init; }
    /// <summary>The timestamp when the conversation was created.</summary>
    public required DateTime CreatedAt { get; init; }
    /// <summary>The timestamp when the last message was added to the conversation.</summary>
    public DateTime LastMessageAt { get; init; }
    /// <summary>A list of messages in the conversation, ordered by creation time.</summary>
    public List<MessageDto> Messages { get; init; } = new();
}
