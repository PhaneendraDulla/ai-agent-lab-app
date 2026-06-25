using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Core.Logging;
using AiAgentLab.Api.Llm.Abstractions;
using AiAgentLab.Api.Llm.Factory;
using AiAgentLab.Api.Llm.Providers;
using AiAgentLab.Api.Services.Chat;
using AiAgentLab.Api.Data;
using AiAgentLab.Api.Tools;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Strongly typed configuration (Options pattern) ---
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(AppSettings.SectionName));
builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection(LlmSettings.SectionName));
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection(OllamaSettings.SectionName));
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection(GeminiSettings.SectionName));

// --- Database (SQL Server via EF Core) ---
var connectionString = builder.Configuration.GetConnectionString("AiAgentLab")
    ?? throw new InvalidOperationException("Connection string 'AiAgentLab' was not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

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

// --- Intent Classification (abstraction for future LLM/embedding-based classification) ---
builder.Services.AddScoped<IIntentClassifier, NoOpIntentClassifier>();
// TODO: Swap to LLMIntentClassifier when ready:
// builder.Services.AddScoped<IIntentClassifier, LLMIntentClassifier>();

// The active provider is chosen by configuration via the factory.
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ILLMProvider>(sp => sp.GetRequiredService<ILLMProviderFactory>().Create());

// Register tools and registry for tool execution loop
builder.Services.AddSingleton<ITool, GetCurrentDateTool>();
builder.Services.AddSingleton<ITool, GetStockPriceTool>();
builder.Services.AddSingleton<IToolRegistry, ToolRegistry>();

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
