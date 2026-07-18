using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Tools;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiAgentLab.Api.Services.Chat;

public sealed class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly IConversationRepository _conversationRepository;
    private readonly IIntentClassifier _intentClassifier;
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<ChatService> _logger;

    private const int MaxToolIterations = 5;

    public ChatService(
        ILLMProvider llmProvider,
        IConversationRepository conversationRepository,
        IIntentClassifier intentClassifier,
        IToolRegistry toolRegistry,
        ILogger<ChatService> logger
    )
    {
        _llmProvider = llmProvider;
        _conversationRepository = conversationRepository;
        _intentClassifier = intentClassifier;
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

    // Backwards-compatible constructor for tests and callers that don't provide a ToolRegistry or logger.
    public ChatService(ILLMProvider llmProvider, IConversationRepository conversationRepository, IIntentClassifier intentClassifier)
        : this(llmProvider, conversationRepository, intentClassifier, new NoOpToolRegistry(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatService>.Instance)
    {
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        // Load or create conversation (same as before)
        var conversation = await _conversationRepository.GetConversationAsync(conversationId, cancellationToken);
        if (conversation == null)
        {
            conversation = new ConversationDto
            {
                Id = conversationId,
                UserId = request.UserId ?? 1,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow,
                Messages = new List<MessageDto>()
            };
            await _conversationRepository.SaveConversationAsync(conversation, cancellationToken);
        }

        var contextMessages = BuildContextWindow(conversation.Messages, maxMessages: 10);
        var intent = request.Intent ?? await ClassifyIntentAsync(request.Message, contextMessages, cancellationToken);

        // Build conversation as LLM messages
        var messages = new List<LLMMessage>
        {
            new LLMMessage { Role = "system", Content = "You are a helpful AI assistant for learning about AI and software development." },
            new LLMMessage { Role = "user", Content = request.Message }
        };

        // Prepare tool declarations
        var toolDecls = _toolRegistry.GetToolDeclarations();

        // Tool loop
        var functionResponses = new List<JsonElement>();

        for (var i = 0; i < MaxToolIterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var llmRequest = new LLMRequest
            {
                Messages = messages,
                ToolDeclarations = toolDecls,
                FunctionResponses = functionResponses
            };

            _logger.LogInformation("Calling Gemini LLM provider (iteration {Iter})", i + 1);
            var llmResponse = await _llmProvider.GenerateAsync(llmRequest, cancellationToken);

            if (llmResponse.HasText)
            {
                _logger.LogInformation(
                    "DECISION (iteration {Iter}): Gemini answered directly — no tool needed.", i + 1);
                var assistantText = llmResponse.Text!;

                // Save messages to conversation history
                await _conversationRepository.AddMessageAsync(conversationId, new MessageDto
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = conversationId,
                    Role = "user",
                    Content = request.Message,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = intent
                }, cancellationToken);

                await _conversationRepository.AddMessageAsync(conversationId, new MessageDto
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = conversationId,
                    Role = "assistant",
                    Content = assistantText,
                    CreatedAt = DateTime.UtcNow,
                    Metadata = new() { { "provider", llmResponse.Provider ?? "unknown" } }
                }, cancellationToken);

                return new ChatResponse
                {
                    Answer = assistantText,
                    ConversationId = conversationId,
                    MessageId = Guid.NewGuid().ToString(),
                    Provider = llmResponse.Provider,
                    Model = llmResponse.Model
                };
            }

            if (llmResponse.HasToolCall)
            {
                var toolCall = llmResponse.ToolCall!;
                _logger.LogInformation(
                    "DECISION (iteration {Iter}): Gemini chose to call tool '{ToolName}' with args {Args}.",
                    i + 1, toolCall.Name, toolCall.Args.ToString());

                // Execute tool via registry
                var toolResult = await _toolRegistry.ExecuteAsync(toolCall.Name, toolCall.Args, cancellationToken);

                if (llmResponse.Provider == "Gemini-Fallback" && llmResponse.Model == "local-fallback")
                {
                    var answer = FormatToolResult(toolResult);
                    await _conversationRepository.AddMessageAsync(conversationId, new MessageDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        ConversationId = conversationId,
                        Role = "user",
                        Content = request.Message,
                        CreatedAt = DateTime.UtcNow,
                        Metadata = intent
                    }, cancellationToken);

                    await _conversationRepository.AddMessageAsync(conversationId, new MessageDto
                    {
                        Id = Guid.NewGuid().ToString(),
                        ConversationId = conversationId,
                        Role = "assistant",
                        Content = answer,
                        CreatedAt = DateTime.UtcNow,
                        Metadata = new() { { "provider", llmResponse.Provider ?? "unknown" } }
                    }, cancellationToken);

                    return new ChatResponse
                    {
                        Answer = answer,
                        ConversationId = conversationId,
                        MessageId = Guid.NewGuid().ToString(),
                        Provider = llmResponse.Provider,
                        Model = llmResponse.Model
                    };
                }

                // Add assistant's function_call representation to messages (for continuity).
                // Store the call arguments as JSON so the provider can echo the functionCall
                // back to Gemini on the next turn.
                messages.Add(new LLMMessage
                {
                    Role = "assistant",
                    Name = toolCall.Name,
                    Content = toolCall.Args.ToString()
                });

                // Add function response to messages and to the functionResponses list sent back to LLM
                messages.Add(new LLMMessage
                {
                    Role = "function",
                    Name = toolCall.Name,
                    Content = toolResult.ToString() ?? string.Empty
                });

                functionResponses.Add(toolResult);

                // Continue loop to call LLM again with function response included
                continue;
            }

            // If neither text nor toolcall, break
            break;
        }

        _logger.LogWarning("Max tool iterations exceeded.");
        return new ChatResponse
        {
            Answer = "I'm sorry — I couldn't complete the request after multiple attempts.",
            ConversationId = conversationId,
            MessageId = Guid.NewGuid().ToString()
        };
    }

    // ... keep ClassifyIntentAsync and BuildContextWindow methods unchanged (copy from existing)
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

    private static string FormatToolResult(JsonElement toolResult)
    {
        if (toolResult.ValueKind == JsonValueKind.Object)
        {
            if (toolResult.TryGetProperty("currentDateTime", out var dateValue))
            {
                return $"The current date and time is {dateValue.GetString()}";
            }

            if (toolResult.TryGetProperty("price", out var priceValue))
            {
                var symbol = toolResult.TryGetProperty("symbol", out var symbolValue)
                    ? symbolValue.GetString() ?? "UNKNOWN"
                    : "UNKNOWN";
                var currency = toolResult.TryGetProperty("currency", out var currencyValue)
                    ? currencyValue.GetString() ?? "USD"
                    : "USD";

                return $"{symbol} is currently priced at {priceValue.GetDecimal():0.00} {currency}.";
            }
        }

        return toolResult.ToString();
    }

    private List<MessageDto> BuildContextWindow(List<MessageDto> messages, int maxMessages)
    {
        return messages.TakeLast(maxMessages).ToList();
    }
}