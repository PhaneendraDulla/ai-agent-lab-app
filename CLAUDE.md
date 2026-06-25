# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

`ai-agent-lab-app` is a personal AI learning lab and a production-style local-first AI agent backend built with **.NET / ASP.NET Core Web API**.

The owner is an experienced **.NET full-stack developer** with C#, ASP.NET Core, Angular, Azure, microservices, and enterprise application experience, but is **new to modern AI engineering** and is learning by building.

Tailor explanations accordingly:

* Do **not** over-explain .NET concepts such as dependency injection, controllers, REST APIs, configuration, middleware, async/await, services, DTOs, or layered architecture.
* Do explain AI-specific terms such as LLMs, embeddings, RAG, vector databases, tool calling, agents, MCP, Ollama, Semantic Kernel, LangChain, LlamaIndex, LangGraph, CrewAI, and AutoGen.
* When useful, compare Python/FastAPI concepts to .NET equivalents:

  * FastAPI routers ≈ ASP.NET Core Controllers
  * FastAPI dependency injection ≈ ASP.NET Core `IServiceCollection`
  * Pydantic models ≈ C# DTOs / records / validation attributes
  * pydantic-settings ≈ strongly typed Options pattern
  * FastAPI lifespan ≈ app startup, hosted services, middleware, dependency registration
  * LLM provider abstraction ≈ registered C# interface in DI
* Default to Windows / PowerShell when shell commands matter. Add bash only when cross-platform behavior matters.

## End-state vision

A customizable AI Research / Investment / Knowledge Assistant that:

* chats with a local Ollama model by default,
* can swap to OpenAI / Claude / Azure OpenAI / Gemini behind the same provider interface,
* uses tool / function calling,
* reads local notes and files,
* has conversation memory,
* does RAG over local documents using embeddings + a vector database,
* exposes a clean ASP.NET Core backend API consumable by .NET services and an Angular UI later,
* supports MCP-style tool integration,
* supports agent orchestration later,
* can run locally first and later in Docker / Azure.

## Goals

* Learn LLM integration using .NET.
* Learn Ollama with ASP.NET Core.
* Learn provider abstraction for multiple AI providers.
* Learn RAG.
* Learn embeddings.
* Learn vector databases.
* Learn tool calling.
* Learn MCP.
* Learn agents.
* Build production-style architecture while learning.

## Milestones

### Milestone 1 current

ASP.NET Core Web API backend with:

* `GET /api/health`
* `POST /api/chat`
* Swagger/OpenAPI
* mock LLM provider first
* Ollama provider integration
* LLM provider abstraction
* service-layer orchestration
* strongly typed configuration
* tool placeholder
* tests

### Milestone 2

* Conversation memory
* Tool execution loop
* Local document ingestion
* Embeddings
* Vector database
* RAG endpoint

### Milestone 3

* Stock / news / research tools
* Angular frontend UI
* MCP server/client exploration
* Agent orchestration framework choice
* Docker
* Azure deployment

When working in this repo, do **not** pre-build features from later milestones unless explicitly asked. The folder structure may leave seams for them, but do not implement them early.

## Architecture

Use normal ASP.NET Core Web API Controllers, not Minimal APIs.

Recommended structure:

```text
AiAgentLabDotNet/
  AiAgentLab.Api/
    Controllers/
      HealthController.cs
      ChatController.cs

    Core/
      Configuration/
        AppSettings.cs
        LlmSettings.cs
        OllamaSettings.cs

      Logging/
        StartupLogger.cs

    Llm/
      Abstractions/
        ILLMProvider.cs
        LLMRequest.cs
        LLMResponse.cs

      Providers/
        MockLLMProvider.cs
        OllamaLLMProvider.cs

      Factory/
        LLMProviderFactory.cs

    Models/
      Chat/
        ChatRequest.cs
        ChatResponse.cs

      Health/
        HealthResponse.cs

    Services/
      Chat/
        IChatService.cs
        ChatService.cs

    Tools/
      ToolPlaceholder.cs

    Program.cs
    appsettings.json
    appsettings.Development.json

  AiAgentLab.Tests/
    Controllers/
    Services/
    Llm/
```

## Python/FastAPI to .NET mapping

```text
Python FastAPI                    .NET ASP.NET Core Web API
----------------------------------------------------------------
app/main.py                       Program.cs
FastAPI app factory               WebApplication builder setup
api/router files                  Controllers
Pydantic request models           C# DTOs / records
pydantic-settings config          Options pattern
LLMProvider abstract base         ILLMProvider interface
provider factory                  LLMProviderFactory or DI registration
ChatService                       ChatService
lifespan startup/shutdown         Program.cs startup / IHostedService
pytest                            xUnit
TestClient                        WebApplicationFactory
.env / .env.example               appsettings.json / user-secrets / env vars
```

## Key design rules

### 1. Provider abstraction is sacred

Controllers and services must depend on `ILLMProvider` or a provider factory abstraction, never directly on concrete providers.

Good:

```csharp
public class ChatService : IChatService
{
    private readonly ILLMProvider _llmProvider;

    public ChatService(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }
}
```

Bad:

```csharp
public class ChatService
{
    private readonly OllamaLLMProvider _ollamaProvider;
}
```

Adding OpenAI, Claude, Azure OpenAI, or Gemini should mean adding a new provider implementation and changing registration/configuration only. Controllers should not change.

### 2. Controllers stay thin

Controllers should only handle HTTP concerns:

* route
* request binding
* response status
* calling service layer

Business orchestration belongs in services.

Good:

```csharp
[HttpPost]
public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
{
    var response = await _chatService.SendAsync(request);
    return Ok(response);
}
```

### 3. Service layer holds orchestration

`ChatService` is the seam where future RAG retrieval, memory load/save, and tool-calling loop will be added.

Do not put orchestration logic in controllers.

### 4. Config flows through Options pattern

All configuration-driven values should come from strongly typed options classes.

Use:

* `IOptions<T>`
* `IOptionsMonitor<T>`
* `builder.Configuration.GetSection(...)`

Do not read environment variables ad-hoc throughout the app.

Good:

```csharp
builder.Services.Configure<LlmSettings>(
    builder.Configuration.GetSection("Llm"));
```

Bad:

```csharp
var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL");
```

### 5. Small files, clear responsibility

Prefer:

* one controller per resource
* one provider per file
* one DTO per file when practical
* one service per use case

Do not create huge files that mix HTTP, AI provider calls, and orchestration.

### 6. appsettings is the contract

Any new configurable value must be added to:

* `appsettings.json`
* `appsettings.Development.json` if needed
* README or setup notes if important

For secrets, use:

* user-secrets locally
* environment variables in deployment
* never hardcode API keys

### 7. Docker is optional in milestone 1

The local development path should be simple:

```powershell
dotnet restore
dotnet build
dotnet run --project .\AiAgentLab.Api\
```

Docker can be added later.

## What NOT to do

* Do not use Minimal APIs for this project.
* Do not put all endpoints in `Program.cs`.
* Do not hardcode model names, base URLs, API keys, or provider-specific request shapes outside provider-specific classes.
* Do not import provider SDKs directly in Controllers.
* Do not add RAG, MemoryStore, Retriever, VectorDb, MCP, or Agent abstractions before milestone 2 or 3 unless explicitly asked.
* Do not introduce Semantic Kernel, LangChain, LlamaIndex, LangGraph, CrewAI, or AutoGen silently. Discuss trade-offs before adopting them.
* Do not add a database layer until a real feature needs it.
* Do not break Windows-friendly setup.
* Do not require Ollama for unit tests.

## Common commands Windows / PowerShell

```powershell
# Create solution
dotnet new sln -n AiAgentLabDotNet

# Create Web API project
dotnet new webapi -n AiAgentLab.Api

# Create test project
dotnet new xunit -n AiAgentLab.Tests

# Add projects to solution
dotnet sln add .\AiAgentLab.Api\AiAgentLab.Api.csproj
dotnet sln add .\AiAgentLab.Tests\AiAgentLab.Tests.csproj

# Add test reference
dotnet add .\AiAgentLab.Tests\AiAgentLab.Tests.csproj reference .\AiAgentLab.Api\AiAgentLab.Api.csproj

# Restore
dotnet restore

# Build
dotnet build

# Run API
dotnet run --project .\AiAgentLab.Api\

# Run tests
dotnet test
```

## Ollama commands

```powershell
# Ollama must be installed and running
ollama pull llama3.2

# Verify Ollama is running
ollama list
```

Expected local Ollama base URL:

```text
http://localhost:11434
```

## API endpoints

Milestone 1 should expose:

```text
GET /api/health
POST /api/chat
```

Example request:

```json
{
  "message": "Explain RAG in simple terms"
}
```

Example response:

```json
{
  "answer": "RAG means Retrieval-Augmented Generation..."
}
```

## Testing

Use xUnit.

Tests should not require Ollama to be running.

Use fake or mock implementations of `ILLMProvider`.

Preferred testing approach:

* Unit test `ChatService` with fake `ILLMProvider`
* Unit test provider-specific behavior by mocking HTTP calls
* Integration test controllers using `WebApplicationFactory`
* Do not call real Ollama or external AI providers in automated tests

## Style and tone

* Code should be beginner-friendly but professional.
* Add short comments only when the reason is not obvious.
* Do not add comments that simply repeat the code.
* Use async-first for I/O.
* Use clear names over clever abstractions.
* Prefer records for simple request/response DTOs.
* Prefer interfaces for services and providers.
* Keep provider-specific request/response models inside the provider area.
* Keep controllers clean and readable.

## Current implementation preference

For the first working version:

* Use Controllers.
* Use Swagger.
* Use `ChatController`.
* Use `HealthController`.
* Use `IChatService`.
* Use `ChatService`.
* Use `ILLMProvider`.
* Use `OllamaLLMProvider` as the default runtime provider.
* The API should call local Ollama at `http://localhost:11434`.
* Default model should be configurable, for example `llama3.2`.
* Use `MockLLMProvider` only for unit tests or fallback examples.
* Do not require external paid APIs.
* Keep future seams for RAG, tools, memory, MCP, and agents, but do not implement them yet.
