using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// Abstraction for classifying user intent from a message.
/// Future implementations can use LLM classification, embeddings-based similarity, or rules.
/// </summary>
public interface IIntentClassifier
{
    /// <summary>
    /// Classify the user's message intent.
    /// </summary>
    /// <param name="message">The user's message to classify.</param>
    /// <param name="conversationHistory">Previous messages for context (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Classified intent with domain, action, and confidence.</returns>
    Task<IntentClassifierResult> ClassifyAsync(
        string message,
        List<MessageDto>? conversationHistory = null,
        CancellationToken cancellationToken = default
    );
}