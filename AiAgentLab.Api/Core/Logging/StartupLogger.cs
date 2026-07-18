using AiAgentLab.Api.Core.Configuration;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Core.Logging;

/// <summary>
/// Logs the resolved configuration once at startup so it is obvious which
/// provider and model the API will use. Helpful while learning / debugging.
/// </summary>
public static class StartupLogger
{
    public static void LogStartupInfo(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var llm = app.Services.GetRequiredService<IOptions<LlmSettings>>().Value;
        var ollama = app.Services.GetRequiredService<IOptions<OllamaSettings>>().Value;

        var gemini = app.Services.GetRequiredService<IOptions<GeminiSettings>>().Value;
        logger.LogInformation("Gemini API key configured: {HasKey}", !string.IsNullOrWhiteSpace(gemini.ApiKey));
        
        logger.LogInformation("AiAgentLab.Api starting. Provider={Provider}", llm.Provider);

        if (string.Equals(llm.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation(
                "Ollama configured. BaseUrl={BaseUrl} Model={Model}",
                ollama.BaseUrl,
                ollama.Model);
        }
    }
}
