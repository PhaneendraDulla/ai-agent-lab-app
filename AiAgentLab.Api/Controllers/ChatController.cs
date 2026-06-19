using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Services.Chat;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentLab.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IConversationRepository _conversationRepository;

    public ChatController(IChatService chatService, IConversationRepository conversationRepository)
    {
        _chatService = chatService;
        _conversationRepository = conversationRepository;
    }

    /// <summary>Send a message and get a response (creates conversation if ConversationId is null).</summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request, CancellationToken cancellationToken)
    {
        // Generate ConversationId if not provided
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        var updatedRequest = request with { ConversationId = conversationId };
        var response = await _chatService.SendAsync(updatedRequest, cancellationToken);
        return Ok(response);
    }

    /// <summary>Retrieve conversation history (last 20 messages).</summary>
    [HttpGet("{conversationId}")]
    public async Task<ActionResult<ConversationHistoryResponse>> GetConversation(
        string conversationId,
        [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetConversationAsync(conversationId, cancellationToken);

        if (conversation == null)
            return NotFound(new { error = "Conversation not found" });

        if (conversation.UserId != userId)
            return Forbid();  // User can only see their own conversations

        // Return last 20 messages
        var recentMessages = conversation.Messages.TakeLast(20).ToList();

        return Ok(new ConversationHistoryResponse
        {
            ConversationId = conversation.Id,
            UserId = conversation.UserId,
            CreatedAt = conversation.CreatedAt,
            LastMessageAt = conversation.LastMessageAt,
            Messages = recentMessages.Select(m => new MessageSummary
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreatedAt
            }).ToList()
        });
    }
}

/// <summary>Response for GET /api/chat/{conversationId}.</summary>
public sealed record ConversationHistoryResponse
{
    public required string ConversationId { get; init; }
    public required string UserId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastMessageAt { get; init; }
    public required List<MessageSummary> Messages { get; init; }
}

/// <summary>Lightweight message info for history response.</summary>
public sealed record MessageSummary
{
    public required string Id { get; init; }
    public required string Role { get; init; }
    public required string Content { get; init; }
    public required DateTime CreatedAt { get; init; }
}