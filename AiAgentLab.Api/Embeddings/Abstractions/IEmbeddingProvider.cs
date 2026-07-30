namespace AiAgentLab.Api.Embeddings.Abstractions;

/// <summary>
/// The single seam every embedding backend implements. Services depend on this
/// abstraction only — never on a concrete provider. Mirrors <see cref="AiAgentLab.Api.Llm.Abstractions.ILLMProvider"/>.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Stable provider name, e.g. "Gemini".</summary>
    string Name { get; }

    /// <summary>Embed a single piece of text into a vector.</summary>
    Task<EmbeddingResponse> EmbedAsync(EmbeddingRequest request, CancellationToken cancellationToken = default);
}
