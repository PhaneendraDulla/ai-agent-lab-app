using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// Default chat orchestration: map the API request to a provider-agnostic
/// <see cref="LLMRequest"/>, call the active provider, and map the result back.
/// Depends on <see cref="ILLMProvider"/> only — never a concrete provider.
/// </summary>
public sealed class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;

    public ChatService(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        // Future milestones add RAG retrieval, memory load/save, and tool calls here.
        var llmRequest = new LLMRequest { Prompt = request.Message };

        var llmResponse = await _llmProvider.GenerateAsync(llmRequest, cancellationToken);

        return new ChatResponse
        {
            Answer = llmResponse.Content,
            Provider = llmResponse.Provider,
            Model = llmResponse.Model
        };
    }
}
