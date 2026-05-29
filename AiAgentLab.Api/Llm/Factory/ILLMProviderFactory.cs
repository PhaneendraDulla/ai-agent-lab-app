using AiAgentLab.Api.Llm.Abstractions;

namespace AiAgentLab.Api.Llm.Factory;

/// <summary>
/// Resolves the active <see cref="ILLMProvider"/> based on configuration.
/// Lets us swap providers via appsettings without touching controllers or services.
/// </summary>
public interface ILLMProviderFactory
{
    ILLMProvider Create();
}
