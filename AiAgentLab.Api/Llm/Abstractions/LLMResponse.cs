using System.Text.Json;

namespace AiAgentLab.Api.Llm.Abstractions
{
    /// <summary>
    /// Represents a tool/function call that the LLM wants to make,
    /// with the tool name and arguments as a JSON object.
    /// </summary>
    public sealed class ToolCall
    {
        // The name of the tool/function to call, which should match one of the registered tools.
        public required string Name { get; init; }
        // The arguments for the tool call, represented as a JSON object. The shape of this object is defined
        public required JsonElement Args { get; init; }
    }

    /// <summary>
    /// Represents the response from the LLM after processing a request,
    /// which may include text content and/or a tool call.
    /// </summary>
    public sealed class LLMResponse
    {
        // The text content of the LLM's response, if any.
        public string? Text { get; init; }
        // A tool call that the LLM wants to make, if any.
        public ToolCall? ToolCall { get; init; }
        // Optional metadata about the model and provider that generated this response, for logging and analytics purposes.
        public string? Model { get; init; }
        public string? Provider { get; init; }

        public bool HasText => !string.IsNullOrWhiteSpace(Text);
        public bool HasToolCall => ToolCall != null;
    }
}