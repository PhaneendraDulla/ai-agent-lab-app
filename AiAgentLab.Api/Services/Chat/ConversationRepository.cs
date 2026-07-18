using AiAgentLab.Api.Data;
using AiAgentLab.Api.Data.Entities;
using AiAgentLab.Api.Models.Chat;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AiAgentLab.Api.Services.Chat;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _ctx;
    public ConversationRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<ConversationDto?> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var entity = await _ctx.Conversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (entity == null) return null;

        return new ConversationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            CreatedAt = entity.CreatedAt,
            LastMessageAt = entity.UpdatedAt,
            Messages = entity.Messages.Select(m => new MessageDto
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt,
                Metadata = m.Metadata != null ? JsonSerializer.Deserialize<Dictionary<string,string>>(m.Metadata) : null
            }).ToList()
        };
    }

    public async Task SaveConversationAsync(ConversationDto conversation, CancellationToken cancellationToken = default)
    {
        var safeUserId = conversation.UserId > 0 ? conversation.UserId : 1;
        await EnsureDefaultUserExistsAsync(cancellationToken);

        var existing = await _ctx.Conversations.FindAsync(new object[] { conversation.Id }, cancellationToken);
        if (existing == null)
        {
            _ctx.Conversations.Add(new ConversationEntity
            {
                Id = conversation.Id,
                UserId = safeUserId,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.LastMessageAt
            });
        }
        else
        {
            existing.UpdatedAt = conversation.LastMessageAt;
        }
        await _ctx.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDefaultUserExistsAsync(CancellationToken cancellationToken)
    {
        await _ctx.Database.ExecuteSqlRawAsync(
            "IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = 1) " +
            "BEGIN " +
            "INSERT INTO dbo.Users (Username, Email, DisplayName, ProviderId, IsActive, CreatedAt, LastSeenAt) " +
            "VALUES ('system-user', NULL, 'System User', 'local-dev', 1, SYSUTCDATETIME(), SYSUTCDATETIME()); " +
            "END",
            cancellationToken);
    }

    public async Task AddMessageAsync(string conversationId, MessageDto message, CancellationToken cancellationToken = default)
    {
        var entity = new MessageEntity
        {
            Id = message.Id,
            ConversationId = conversationId,
            Role = message.Role,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            IntentDomain = message.Metadata?.GetValueOrDefault("domain"),
            IntentAction = message.Metadata?.GetValueOrDefault("action"),
            Metadata = message.Metadata != null ? JsonSerializer.Serialize(message.Metadata) : null
        };
        _ctx.Messages.Add(entity);

        var conv = await _ctx.Conversations.FindAsync(new object[] { conversationId }, cancellationToken);
        if (conv != null) conv.UpdatedAt = DateTime.UtcNow;

        await _ctx.SaveChangesAsync(cancellationToken);
    }
}