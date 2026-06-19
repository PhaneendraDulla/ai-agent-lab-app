using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// Default no-op intent classifier that returns generic intent.
/// Used during development; can be swapped for LLM-based or embedding-based classifiers later.
/// </summary>
public sealed class NoOpIntentClassifier : IIntentClassifier
{
    public Task<IntentClassifierResult> ClassifyAsync(
        string message,
        List<MessageDto>? conversationHistory = null,
        CancellationToken cancellationToken = default
    )
    {
        // For now, return a generic intent
        // Later: swap this for LLM classification, embedding similarity, or rule-based logic
        var result = new IntentClassifierResult
        {
            Domain = "general",
            Action = "query",
            Confidence = 0.5,  // Low confidence to indicate this is a placeholder
            Metadata = new() { { "classifier", "no_op" } }
        };

        return Task.FromResult(result);
    }
}