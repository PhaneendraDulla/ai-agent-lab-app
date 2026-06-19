using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// Repository for storing and retrieving conversations and messages. This is a seam for future
/// </summary>
public interface IConversationRepository
{
    Task<ConversationDto?> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    Task SaveConversationAsync(ConversationDto conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(string conversationId, MessageDto message, CancellationToken cancellationToken = default);
}