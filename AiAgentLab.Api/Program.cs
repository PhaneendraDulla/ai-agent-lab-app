using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Core.Logging;
using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Llm.Factory;
using AiAgentLab.Api.Llm.Providers;
using AiAgentLab.Api.Services.Chat;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// --- Strongly typed configuration (Options pattern) ---
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection(LlmSettings.SectionName));
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection(OllamaSettings.SectionName));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection(GeminiSettings.SectionName));

// --- LLM providers ---
// Mock is registered for tests/fallback; Ollama is the default runtime provider.
builder.Services.AddSingleton<MockLLMProvider>();
builder.Services.AddHttpClient<OllamaLLMProvider>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

builder.Services.AddHttpClient<GeminiLLMProvider>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<GeminiSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// The active provider is chosen by configuration via the factory.
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ILLMProvider>(sp => sp.GetRequiredService<ILLMProviderFactory>().Create());

// --- Application services ---
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton(TimeProvider.System);

// --- Web API + Swagger ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

StartupLogger.LogStartupInfo(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Exposed so WebApplicationFactory<Program> can host the app in integration tests.
public partial class Program;
