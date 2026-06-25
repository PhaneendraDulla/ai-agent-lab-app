using System.Text.Json;

namespace AiAgentLab.Api.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        /// <summary>
        /// Declaration object that can be serialized to the LLM's 'function_declarations' or similar.
        /// Example shape: { "name": "...", "description": "...", "parameters": {...} }
        /// </summary>
        object GetDeclaration();
        /// <summary>
        /// Execute the tool and return a JSON result that will be sent back to the LLM as functionResponse.
        /// </summary>
        Task<JsonElement> ExecuteAsync(JsonElement args, CancellationToken cancellationToken);
    }
}