using System.Text.Json;

namespace AiAgentLab.Api.Llm.Abstractions
{
    /// <summary>
    /// Represents a single message in the conversation, 
    /// with a role (user, assistant, function), 
    /// optional name (for functions), and content.
    /// </summary>
    public sealed record LLMMessage
    {
        // The role of the message sender, e.g. "user", "assistant", or "function" (for tool calls).
        public required string Role { get; init; } 
        // The name of the function/tool being called, if Role == "function" or assistant's function_call. Null for user messages.
        public string? Name { get; init; } 
        // The content of the message. For user and assistant messages, this is the text content.
        // For function messages, this is the JSON-serialized arguments.
        public required string Content { get; init; }
    }

    /// <summary>
    /// Represents the request body sent to the LLM provider, 
    /// containing the conversation messages, 
    /// tool/function declarations, and 
    /// any prior function responses. 
    /// The exact shape of ToolDeclarations and FunctionResponses is provider-specific and 
    /// should be constructed by the service layer based on the registered tools and conversation history.
    /// </summary>
    public sealed record LLMRequest
    {
        // The messages making up the conversation in order.
        public required IReadOnlyList<LLMMessage> Messages { get; init; }

        // Tool/function declarations (provider-specific shape) to send to the LLM.
        public IReadOnlyList<object>? ToolDeclarations { get; init; }

        // Any prior function responses to include when calling the LLM (provider-specific).
        public IReadOnlyList<JsonElement>? FunctionResponses { get; init; }
    }
}