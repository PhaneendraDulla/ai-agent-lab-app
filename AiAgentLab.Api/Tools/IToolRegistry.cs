using System.Text.Json;

namespace AiAgentLab.Api.Tools
{
    public interface IToolRegistry
    {
        IReadOnlyList<object> GetToolDeclarations();
        ITool? GetTool(string name);
        Task<JsonElement> ExecuteAsync(string name, JsonElement args, CancellationToken cancellationToken);
    }
}