using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Llm.Providers;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Llm.Factory;

/// <summary>
/// Picks a provider implementation by name from <see cref="LlmSettings.Provider"/>.
/// Concrete providers are resolved from DI so their own dependencies
/// (HttpClient, options, loggers) are wired correctly.
/// </summary>
public sealed class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LlmSettings _settings;

    public LLMProviderFactory(IServiceProvider serviceProvider, IOptions<LlmSettings> settings)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
    }

    public ILLMProvider Create()
    {
        return _settings.Provider.Trim().ToLowerInvariant() switch
        {
            "ollama" => _serviceProvider.GetRequiredService<OllamaLLMProvider>(),
            "mock" => _serviceProvider.GetRequiredService<MockLLMProvider>(),
            "gemini" => _serviceProvider.GetRequiredService<GeminiLLMProvider>(),
            _ => throw new InvalidOperationException(
                $"Unknown LLM provider '{_settings.Provider}'. Valid values: Ollama, Mock, Gemini.")
        };
    }
}
