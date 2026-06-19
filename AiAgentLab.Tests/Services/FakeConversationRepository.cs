using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Services.Chat;

namespace AiAgentLab.Tests.Services;

public sealed class FakeConversationRepository : IConversationRepository
{
    private readonly Dictionary<string, ConversationDto> _store = new();

    public Task<ConversationDto?> GetConversationAsync(
        string conversationId,
        CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(conversationId, out var conv);
        return Task.FromResult(conv);
    }

    public Task SaveConversationAsync(
        ConversationDto conversation,
        CancellationToken cancellationToken = default)
    {
        _store[conversation.Id] = conversation;
        return Task.CompletedTask;
    }

    public Task AddMessageAsync(
        string conversationId,
        MessageDto message,
        CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(conversationId, out var conv))
            conv.Messages.Add(message);

        return Task.CompletedTask;
    }
}