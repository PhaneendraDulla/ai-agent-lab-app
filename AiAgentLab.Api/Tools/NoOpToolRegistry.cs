using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AiAgentLab.Api.Tools;

public sealed class NoOpToolRegistry : IToolRegistry
{
    public IReadOnlyList<object> GetToolDeclarations() => Array.Empty<object>();

    public ITool? GetTool(string name) => null;

    public Task<JsonElement> ExecuteAsync(string name, JsonElement args, CancellationToken cancellationToken)
    {
        var err = new { error = "tool_not_found", name };
        return Task.FromResult(JsonSerializer.SerializeToElement(err));
    }
}
