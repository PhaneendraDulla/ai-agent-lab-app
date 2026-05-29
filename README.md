# ai-agent-lab-app

A personal AI learning lab and a production-style, local-first AI agent backend
built with .NET / ASP.NET Core Web API.

This is **Milestone 1**: a clean Web API with a swappable LLM provider abstraction,
defaulting to a local [Ollama](https://ollama.com) model.

## Solution layout

```
AiAgentLabApp.sln
  AiAgentLab.Api/      ASP.NET Core Web API (controllers, services, LLM providers)
  AiAgentLab.Tests/    xUnit unit + integration tests (no Ollama required)
```

Key seams (see [CLAUDE.md](CLAUDE.md) for the full design rules):

- `ILLMProvider` — the abstraction every backend implements (`MockLLMProvider`, `OllamaLLMProvider`).
- `ILLMProviderFactory` — selects the active provider from configuration.
- `IChatService` / `ChatService` — orchestration seam for future RAG, memory, and tool calls.

## Endpoints

| Method | Route          | Description                       |
| ------ | -------------- | --------------------------------- |
| GET    | `/api/health`  | Liveness + app metadata           |
| POST   | `/api/chat`    | Send a message to the active LLM  |

`POST /api/chat` request / response:

```json
{ "message": "Explain RAG in simple terms" }
```

```json
{ "answer": "RAG means Retrieval-Augmented Generation...", "provider": "Ollama", "model": "llama3.2" }
```

Swagger UI is available at `/swagger` in the Development environment.

## Run locally (Windows / PowerShell)

```powershell
dotnet restore
dotnet build
dotnet run --project .\AiAgentLab.Api\
```

Ollama must be installed and running for the default provider:

```powershell
ollama pull llama3.2
ollama list   # verify it is running at http://localhost:11434
```

To run without Ollama, switch to the mock provider via configuration:

```powershell
$env:Llm__Provider = "Mock"
dotnet run --project .\AiAgentLab.Api\
```

## Configuration

All settings flow through the strongly typed Options pattern. Defaults live in
[`appsettings.json`](AiAgentLab.Api/appsettings.json):

| Section   | Key             | Default                  | Purpose                          |
| --------- | --------------- | ------------------------ | -------------------------------- |
| `App`     | `Name`          | `AiAgentLab.Api`         | Reported by `/api/health`        |
| `App`     | `Version`       | `0.1.0`                  | Reported by `/api/health`        |
| `Llm`     | `Provider`      | `Ollama`                 | Active provider (`Ollama`/`Mock`)|
| `Ollama`  | `BaseUrl`       | `http://localhost:11434` | Local Ollama server              |
| `Ollama`  | `Model`         | `llama3.2`               | Chat model                       |
| `Ollama`  | `TimeoutSeconds`| `120`                    | HTTP timeout for Ollama calls    |

## Test

```powershell
dotnet test
```

Tests use a fake/mock `ILLMProvider` and never call a real model or network.
```
