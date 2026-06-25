using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AiAgentLab.Api.Tools
{
    public sealed class ToolRegistry : IToolRegistry
    {
        private readonly Dictionary<string, ITool> _byName;
        private readonly ILogger<ToolRegistry> _logger;

        public ToolRegistry(IEnumerable<ITool> tools, ILogger<ToolRegistry> logger)
        {
            _logger = logger;
            _byName = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<object> GetToolDeclarations()
        {
            return _byName.Values.Select(t => t.GetDeclaration()).ToList();
        }

        public ITool? GetTool(string name)
        {
            return _byName.TryGetValue(name, out var t) ? t : null;
        }

        public async Task<JsonElement> ExecuteAsync(string name, JsonElement args, CancellationToken cancellationToken)
        {
            if (!_byName.TryGetValue(name, out var tool))
            {
                var err = new { error = "tool_not_found", name };
                _logger.LogWarning("Tool not found: {ToolName}", name);
                return JsonSerializer.SerializeToElement(err);
            }

            _logger.LogInformation("Executing tool {ToolName}", name);
            try
            {
                var res = await tool.ExecuteAsync(args, cancellationToken);
                _logger.LogInformation("Tool {ToolName} completed", name);
                return res;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool {ToolName} failed", name);
                var err = new { error = "tool_error", name, message = ex.Message };
                return JsonSerializer.SerializeToElement(err);
            }
        }
    }
}