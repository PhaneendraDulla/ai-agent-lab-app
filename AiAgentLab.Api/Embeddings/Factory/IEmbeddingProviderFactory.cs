using AiAgentLab.Api.Embeddings.Abstractions;

namespace AiAgentLab.Api.Embeddings.Factory;

/// <summary>
/// Resolves the active <see cref="IEmbeddingProvider"/>. Mirrors ILLMProviderFactory
/// so swapping embedding backends later means adding an implementation and a config value.
/// </summary>
public interface IEmbeddingProviderFactory
{
    IEmbeddingProvider Create();
}
