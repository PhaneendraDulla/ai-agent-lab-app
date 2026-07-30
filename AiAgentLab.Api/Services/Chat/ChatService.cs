using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Embeddings.Abstractions;
using AiAgentLab.Api.Embeddings.Providers;
using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Models.Chat;
using AiAgentLab.Api.Tools;
using AiAgentLab.Api.VectorStore.Abstractions;
using AiAgentLab.Api.VectorStore.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AiAgentLab.Api.Services.Chat;

public sealed class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;
    private readonly IConversationRepository _conversationRepository;
    private readonly IIntentClassifier _intentClassifier;
    private readonly IToolRegistry _toolRegistry;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly VectorStoreSettings _vectorStoreSettings;
    private readonly ILogger<ChatService> _logger;

    private const int MaxToolIterations = 5;

    public ChatService(
        ILLMProvider llmProvider,
        IConversationRepository conversationRepository,
        IIntentClassifier intentClassifier,
        IToolRegistry toolRegistry,
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        IOptions<VectorStoreSettings> vectorStoreSettings,
        ILogger<ChatService> logger
    )
    {
        _llmProvider = llmProvider;
        _conversationRepository = conversationRepository;
        _intentClassifier = intentClassifier;
        _toolRegistry = toolRegistry;
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _vectorStoreSettings = vectorStoreSettings.Value;
        _logger = logger;
    }

    // Backwards-compatible constructor for tests and callers that don't provide a
    // ToolRegistry, RAG dependencies, or logger — RAG retrieval becomes a no-op.
    public ChatService(ILLMProvider llmProvider, IConversationRepository conversationRepository, IIntentClassifier intentClassifier)
        : this(
            llmProvider,
            conversationRepository,
            intentClassifier,
            new NoOpToolRegistry(),
            new NoOpEmbeddingProvider(),
            new NoOpVectorStore(),
            Options.Create(new VectorStoreSettings()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatService>.Instance)
    {
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        // Marks the start of this turn's log block so MEMORY/INTENT/DECISION/TOOL
        // lines that follow can be traced back to the question that triggered them.
        _logger.LogInformation(
            "REQUEST: conversation {ConversationId} — \"{Message}\"", conversationId, request.Message);

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
        _logger.LogInformation(
            "MEMORY: loaded {Count} prior message(s) as context for conversation {ConversationId}.",
            contextMessages.Count, conversationId);
        var intent = await ClassifyIntentAsync(request.Message, contextMessages, cancellationToken);
        var ragContext = await RetrieveRagContextAsync(request.Message, conversationId, cancellationToken);

        // Build conversation as LLM messages.
        // Start with the system prompt, then replay recent history so the model has
        // memory of earlier turns, and finish with the current user message.
        var systemPrompt = "You are a helpful AI assistant for learning about AI and software development.";
        if (!string.IsNullOrWhiteSpace(ragContext))
        {
            systemPrompt += $"\n\nRelevant context:\n{ragContext}";
        }

        var messages = new List<LLMMessage>
        {
            new LLMMessage { Role = "system", Content = systemPrompt }
        };

        messages.AddRange(contextMessages.Select(m => new LLMMessage
        {
            Role = m.Role,
            Content = m.Content
        }));

        messages.Add(new LLMMessage { Role = "user", Content = request.Message });

        // Prepare tool declarations
        var toolDecls = _toolRegistry.GetToolDeclarations();

        // Tool loop
        var functionResponses = new List<JsonElement>();

        // Track which tools (if any) the model actually invoked during this request,
        // so we can log a single clear summary before returning.
        var toolsInvoked = new List<string>();

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
                LogToolSummary(conversationId, toolsInvoked);
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
                toolsInvoked.Add(toolCall.Name);
                _logger.LogInformation(
                    "DECISION (iteration {Iter}): Gemini chose to call tool '{ToolName}' with args {Args}.",
                    i + 1, toolCall.Name, toolCall.Args.ToString());

                // Execute tool via registry
                var toolResult = await _toolRegistry.ExecuteAsync(toolCall.Name, toolCall.Args, cancellationToken);

                if (llmResponse.Provider == "Gemini-Fallback" && llmResponse.Model == "local-fallback")
                {
                    LogToolSummary(conversationId, toolsInvoked);
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
        LogToolSummary(conversationId, toolsInvoked);
        return new ChatResponse
        {
            Answer = "I'm sorry — I couldn't complete the request after multiple attempts.",
            ConversationId = conversationId,
            MessageId = Guid.NewGuid().ToString()
        };
    }

    // Embeds the user's message and retrieves the top-K most similar chunks from the
    // vector store, formatted for injection into the system prompt. Retrieval failures
    // (e.g. an embedding API hiccup) are logged and swallowed rather than failing the
    // whole chat turn — RAG context is an enhancement, not a hard dependency of chat.
    private async Task<string?> RetrieveRagContextAsync(string message, string conversationId, CancellationToken cancellationToken)
    {
        try
        {
            var embedding = await _embeddingProvider.EmbedAsync(new EmbeddingRequest { Text = message }, cancellationToken);
            if (embedding.Vector.Length == 0)
                return null;

            var results = await _vectorStore.SearchAsync(embedding.Vector, _vectorStoreSettings.TopK, cancellationToken);

            // TEMP DEBUG (remove once RAG tuning is done): dump every candidate chunk and
            // its cosine similarity score to the query, before the MinScore filter runs.
            for (var i = 0; i < results.Count; i++)
            {
                var r = results[i];
                var preview = r.Chunk.Text.Length > 120 ? r.Chunk.Text[..120] + "..." : r.Chunk.Text;
                _logger.LogInformation(
                    "RAG DEBUG: candidate #{Rank} — [{Document} #{ChunkIndex}] score={Score:F4} text=\"{Preview}\"",
                    i + 1, r.Chunk.DocumentName, r.Chunk.ChunkIndex, r.Score, preview);
            }

            var relevant = results.Where(r => r.Score >= _vectorStoreSettings.MinScore).ToList();
            if (relevant.Count == 0)
            {
                _logger.LogInformation(
                    "RAG: {Total} chunk(s) retrieved but none met the {MinScore} similarity threshold for conversation {ConversationId}.",
                    results.Count, _vectorStoreSettings.MinScore, conversationId);
                return null;
            }

            results = relevant;

            _logger.LogInformation(
                "RAG: retrieved {Count} chunk(s) above the {MinScore} threshold for conversation {ConversationId}.",
                results.Count, _vectorStoreSettings.MinScore, conversationId);

            return string.Join(
                "\n\n",
                results.Select(r => $"[{r.Chunk.DocumentName} #{r.Chunk.ChunkIndex}]\n{r.Chunk.Text}"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RAG: retrieval failed for conversation {ConversationId}; continuing without context.", conversationId);
            return null;
        }
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

    // Emits a single, easy-to-scan summary of whether any tool was hit during the request.
    private void LogToolSummary(string conversationId, List<string> toolsInvoked)
    {
        if (toolsInvoked.Count == 0)
        {
            _logger.LogInformation(
                "TOOL SUMMARY (conversation {ConversationId}): no tools were used.", conversationId);
        }
        else
        {
            _logger.LogInformation(
                "TOOL SUMMARY (conversation {ConversationId}): {ToolCount} tool call(s) — {Tools}.",
                conversationId, toolsInvoked.Count, string.Join(", ", toolsInvoked));
        }
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