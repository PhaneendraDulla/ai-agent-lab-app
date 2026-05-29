using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// Orchestrates a chat turn. This is the seam where future RAG retrieval,
/// conversation memory, and the tool-calling loop will be added.
/// </summary>
public interface IChatService
{
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
