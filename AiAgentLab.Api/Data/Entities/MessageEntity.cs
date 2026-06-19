namespace AiAgentLab.Api.Data.Entities;

/// <summary>
/// Represents a single message in a conversation. Contains the role (user, assistant, system), content, and metadata for intent classification.
/// </summary>
public class MessageEntity
{
    // Primary key for the message 
    public string Id { get; set; } = null!;
    // Foreign key to the conversation this message belongs to
    public string ConversationId { get; set; } = null!;
    // Role of the message sender (e.g., "user", "assistant", "system")
    public string Role { get; set; } = null!;
    // The actual content of the message
    public string Content { get; set; } = null!;
    // Timestamp when the message was created
    public DateTime CreatedAt { get; set; }

    // Optional fields for intent classification results
    public string? IntentDomain { get; set; }
    // Optional field for the specific action within the intent domain (e.g., "get_weather" in "weather")
    public string? IntentAction { get; set; }
    // Optional JSON string for any additional metadata (e.g., extracted entities, confidence scores)
    public string? Metadata { get; set; }

    // Navigation property to the parent conversation
    public ConversationEntity? Conversation { get; set; }
}