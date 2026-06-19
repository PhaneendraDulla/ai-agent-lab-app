using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// Chat orchestration service: loads conversation history, builds context,
/// calls LLM, classifies intent, and persists messages.
/// Depends on ILLMProvider and IIntentClassifier abstractions for flexibility.
/// </summary>
public sealed class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly IConversationRepository _conversationRepository;
    private readonly IIntentClassifier _intentClassifier;

    public ChatService(
        ILLMProvider llmProvider,
        IConversationRepository conversationRepository,
        IIntentClassifier intentClassifier
    )
    {
        _llmProvider = llmProvider;
        _conversationRepository = conversationRepository;
        _intentClassifier = intentClassifier;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        // 1. Load or create conversation
        var conversation = await _conversationRepository.GetConversationAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            conversation = new ConversationDto
            {
                Id = conversationId,
                UserId = request.UserId ?? 0,   // 0 = anonymous; real users start at 1
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow,
                Messages = new List<MessageDto>()
            };

            await _conversationRepository.SaveConversationAsync(conversation, cancellationToken);
        }

        // 2. Build context from history (last N messages to fit token budget)
        var contextMessages = BuildContextWindow(conversation.Messages, maxMessages: 10);

        // 3. Classify intent (can be overridden by explicit intent in request)
        var intent = request.Intent ?? await ClassifyIntentAsync(request.Message, contextMessages, cancellationToken);

        // 4. Build LLM request with system prompt + history + new message
        var llmRequest = BuildLLMRequest(contextMessages, request.Message);

        // 5. Call LLM
        var llmResponse = await _llmProvider.GenerateAsync(llmRequest, cancellationToken);

        // 6. Save user message to history with classified intent
        await _conversationRepository.AddMessageAsync(
            conversationId,
            new MessageDto
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Role = "user",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow,
                Metadata = intent
            },
            cancellationToken
        );

        // 7. Save assistant response to history
        await _conversationRepository.AddMessageAsync(
            conversationId,
            new MessageDto
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Role = "assistant",
                Content = llmResponse.Content,
                CreatedAt = DateTime.UtcNow,
                Metadata = new() { { "provider", llmResponse.Provider ?? "unknown" } }
            },
            cancellationToken
        );

        return new ChatResponse
        {
            Answer = llmResponse.Content,
            ConversationId = conversationId,
            MessageId = Guid.NewGuid().ToString(),
            Provider = llmResponse.Provider,
            Model = llmResponse.Model
        };
    }

    private async Task<Dictionary<string, string>> ClassifyIntentAsync(
        string message,
        List<MessageDto> contextMessages,
        CancellationToken cancellationToken
    )
    {
        var classification = await _intentClassifier.ClassifyAsync(message, contextMessages, cancellationToken);

        return new Dictionary<string, string>
        {
            { "domain", classification.Domain },
            { "action", classification.Action },
            { "confidence", classification.Confidence.ToString("F2") },
            { "classifier", classification.Metadata?.GetValueOrDefault("classifier") ?? "unknown" }
        };
    }

    private List<MessageDto> BuildContextWindow(List<MessageDto> messages, int maxMessages)
    {
        return messages.TakeLast(maxMessages).ToList();
    }

    private LLMRequest BuildLLMRequest(List<MessageDto> context, string newMessage)
    {
        var systemPrompt = "You are a helpful AI assistant for learning about AI and software development.";

        var formattedHistory = string.Join("\n", context.Select(m =>
            $"{m.Role.ToUpper()}: {m.Content}"
        ));

        var fullPrompt = $"{systemPrompt}\n\nConversation history:\n{formattedHistory}\n\nUSER: {newMessage}";
        return new LLMRequest { Prompt = fullPrompt };
    }
}