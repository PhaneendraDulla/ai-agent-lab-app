using AiAgentLab.Api.Models.Chat;

namespace AiAgentLab.Api.Services.Chat;

/// <summary>
/// LLM-based intent classifier (future implementation).
/// Will use the active LLM provider to classify user intent.
/// 
/// Usage: Register in Program.cs as:
///   builder.Services.AddScoped<IIntentClassifier, LLMIntentClassifier>();
/// </summary>
public sealed class LLMIntentClassifier : IIntentClassifier
{
    // TODO: Implement when ready to learn LLM-based classification
    // Suggested approach:
    // 1. Call LLM with prompt: "Classify this message intent into domain (e.g., 'rag', 'embeddings', 'debugging') and action (e.g., 'explain', 'code_example')"
    // 2. Parse LLM response as JSON
    // 3. Return IntentClassifierResult with confidence from LLM

    public Task<IntentClassifierResult> ClassifyAsync(
        string message,
        List<MessageDto>? conversationHistory = null,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException("Implement when ready to add LLM-based intent classification.");
    }
}