using System.Text.Json;
using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Models.Chat;
using Microsoft.Extensions.Logging;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// LLM-based intent classifier. Sends the user's message (plus a little recent
/// history for disambiguation) to the active <see cref="ILLMProvider"/> with a
/// prompt asking it to return strict JSON, then parses that JSON into an
/// <see cref="IntentClassifierResult"/>.
///
/// Classification is best-effort: if the call fails or the model doesn't return
/// parseable JSON, this falls back to a low-confidence generic result instead of
/// throwing, so a flaky classification never breaks the main chat response.
/// </summary>
public sealed class LLMIntentClassifier : IIntentClassifier
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<LLMIntentClassifier> _logger;

    private static readonly string[] KnownDomains =
        ["rag", "embeddings", "tool_calling", "mcp", "agents", "architecture", "debugging", "general"];

    private static readonly string[] KnownActions =
        ["explain", "code_example", "troubleshoot", "compare", "query"];

    public LLMIntentClassifier(ILLMProvider llmProvider, ILogger<LLMIntentClassifier> logger)
    {
        _llmProvider = llmProvider;
        _logger = logger;
    }

    public async Task<IntentClassifierResult> ClassifyAsync(
        string message,
        List<MessageDto>? conversationHistory = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var messages = new List<LLMMessage>
            {
                new() { Role = "system", Content = BuildSystemPrompt() }
            };

            // A couple of recent turns is enough to disambiguate follow-ups like
            // "what about the second one" without blowing up the classification prompt.
            var historyIncluded = 0;
            if (conversationHistory is { Count: > 0 })
            {
                foreach (var historyMessage in conversationHistory.TakeLast(4))
                {
                    messages.Add(new LLMMessage { Role = historyMessage.Role, Content = historyMessage.Content });
                    historyIncluded++;
                }
            }

            _logger.LogInformation(
                "INTENT: including {Count} prior message(s) in classification prompt.", historyIncluded);

            messages.Add(new LLMMessage { Role = "user", Content = message });

            var request = new LLMRequest { Messages = messages };
            var response = await _llmProvider.GenerateAsync(request, cancellationToken);

            var result = ParseResult(response.Text);
            if (result is not null)
            {
                _logger.LogInformation(
                    "INTENT: classified as {Domain}/{Action} (confidence {Confidence}).",
                    result.Domain, result.Action, result.Confidence);
                return result;
            }

            _logger.LogWarning(
                "INTENT FALLBACK: could not parse a JSON result from the model response. Falling back to generic intent.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "INTENT FALLBACK: classification call failed. Falling back to generic intent.");
        }

        return new IntentClassifierResult
        {
            Domain = "general",
            Action = "query",
            Confidence = 0.5,
            Metadata = new() { { "classifier", "llm_fallback" } }
        };
    }

    private static string BuildSystemPrompt()
    {
        return """
            You classify the intent of a user's message in an AI/software-engineering learning assistant.

            Respond with ONLY a single JSON object, no markdown fences and no extra text, in this exact shape:
            {"domain": "<domain>", "action": "<action>", "confidence": <0.0-1.0>}
            """
            + "\n\ndomain must be one of: " + string.Join(", ", KnownDomains)
            + "\naction must be one of: " + string.Join(", ", KnownActions)
            + "\nconfidence is your certainty in the classification, as a number between 0 and 1.";
    }

    private static IntentClassifierResult? ParseResult(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var json = ExtractJsonObject(text);
        if (json is null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var domain = root.TryGetProperty("domain", out var domainValue) ? domainValue.GetString() : null;
            var action = root.TryGetProperty("action", out var actionValue) ? actionValue.GetString() : null;
            var confidence = root.TryGetProperty("confidence", out var confidenceValue)
                && confidenceValue.ValueKind is JsonValueKind.Number
                ? confidenceValue.GetDouble()
                : 0.5;

            if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(action))
                return null;

            if (!KnownDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                domain = "general";

            if (!KnownActions.Contains(action, StringComparer.OrdinalIgnoreCase))
                action = "query";

            return new IntentClassifierResult
            {
                Domain = domain,
                Action = action,
                Confidence = Math.Clamp(confidence, 0.0, 1.0),
                Metadata = new() { { "classifier", "llm" } }
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Models sometimes wrap JSON in ```json ... ``` fences or add a stray sentence
    // around it despite instructions; pull out the first {...} block defensively.
    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }
}
