using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Services.Chat;
using Microsoft.AspNetCore.Mvc;

namespace AiAgentLab.Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request, CancellationToken cancellationToken)
    {
        var response = await _chatService.SendAsync(request, cancellationToken);
        return Ok(response);
    }
}
