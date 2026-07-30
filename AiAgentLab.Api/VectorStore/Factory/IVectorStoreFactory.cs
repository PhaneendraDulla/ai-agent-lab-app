using AiAgentLab.Api.VectorStore.Abstractions;

namespace AiAgentLab.Api.VectorStore.Factory;

/// <summary>
/// Resolves the active <see cref="IVectorStore"/> based on configuration.
/// Lets us swap backends via appsettings without touching controllers or services.
/// </summary>
public interface IVectorStoreFactory
{
    IVectorStore Create();
}
