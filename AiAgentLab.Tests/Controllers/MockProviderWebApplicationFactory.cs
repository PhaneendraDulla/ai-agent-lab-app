using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AiAgentLab.Tests.Controllers;

/// <summary>
/// Hosts the real API in-memory but forces the Mock LLM provider, so controller
/// integration tests never require Ollama or any network call.
/// </summary>
public sealed class MockProviderWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Llm:Provider"] = "Mock"
            });
        });
    }
}
