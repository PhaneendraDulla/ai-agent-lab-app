namespace AiAgentLab.Api.Data.Entities;

/// <summary>
/// Represents a conversation between a user and the AI agent. 
/// Contains metadata about the conversation and a collection of messages.
/// </summary>
public class ConversationEntity
{
    // Primary key for the conversation
    public string Id { get; set; } = null!;
    // Identifier for the user who owns this conversation
    public string UserId { get; set; } = null!;
    // Timestamp when the conversation was created
    public DateTime CreatedAt { get; set; }
    // Timestamp when the conversation was last updated (e.g., when a new message is added)
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property for the messages in this conversation
    public ICollection<MessageEntity> Messages { get; set; } = new List<MessageEntity>();
}