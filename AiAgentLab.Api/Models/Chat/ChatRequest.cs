using System.ComponentModel.DataAnnotations;

namespace AiAgentLab.Api.Models.Chat;

/// <summary>Incoming chat request body for <c>POST /api/chat</c>.</summary>
public sealed record ChatRequest
{
    /// <summary>The user's message to send to the model.</summary>
    [Required(AllowEmptyStrings = false)]
    public required string Message { get; init; }
}
