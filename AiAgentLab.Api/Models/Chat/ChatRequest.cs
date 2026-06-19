using System.ComponentModel.DataAnnotations;

namespace AiAgentLab.Api.Models.Chat;

/// <summary>Incoming chat request body for <c>POST /api/chat</c>.</summary>
public sealed record ChatRequest
{
    /// <summary>The user's message to send to the model.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Message { get; init; }  = string.Empty;

    /// <summary>Identifies the conversation. If null, server generates a new ID.</summary>
    public string? ConversationId { get; init; }

    /// <summary>User identifier.</summary>
    public required int? UserId { get; init; }

    /// <summary>Optional user intent metadata for classification and analytics.</summary>
    public Dictionary<string, string>? Intent { get; init; }
}