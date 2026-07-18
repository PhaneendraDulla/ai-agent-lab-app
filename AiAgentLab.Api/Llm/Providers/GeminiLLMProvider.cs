using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiAgentLab.Api.Core.Configuration;
using AiAgentLab.Api.Llm.Abstractions;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Llm.Providers;

public sealed class GeminiLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiLLMProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public GeminiLLMProvider(
        HttpClient httpClient,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiLLMProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string Name => "Gemini";

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(request.Messages);

        if (ShouldUseLocalFallback(prompt))
        {
            _logger.LogInformation("Using local fallback path for prompt that looks like a tool-style request.");
            return CreateFallbackResponse(prompt, request.Messages);
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Returning a local fallback response.");
            return CreateFallbackResponse(prompt, request.Messages);
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.Model}:generateContent";

        var body = new Dictionary<string, object?>
        {
            ["contents"] = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        _logger.LogInformation("Gemini request URL: {Url}", url);
        _logger.LogInformation("Sending generateContent request to Gemini model {Model}", _settings.Model);

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("x-goog-api-key", _settings.ApiKey);
            httpRequest.Content = JsonContent.Create(body, options: _jsonOptions);

            using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
            httpResponse.EnsureSuccessStatusCode();

            var resp = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(_jsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Gemini returned an empty response body.");

            var candidate = resp.Candidates?.FirstOrDefault();
            var functionCall = ExtractFunctionCall(candidate);

            if (functionCall is not null)
            {
                return new LLMResponse
                {
                    ToolCall = new ToolCall
                    {
                        Name = functionCall.Name ?? string.Empty,
                        Args = NormalizeArguments(functionCall.Args)
                    },
                    Model = _settings.Model,
                    Provider = Name
                };
            }

            var text = candidate?.Content?.Parts?.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?.Text ?? string.Empty;

            return new LLMResponse
            {
                Text = text,
                Model = _settings.Model,
                Provider = Name
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Gemini request failed. Returning a local fallback response.");
            return CreateFallbackResponse(prompt, request.Messages);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Gemini request timed out. Returning a local fallback response.");
            return CreateFallbackResponse(prompt, request.Messages);
        }
    }

    private static bool ShouldUseLocalFallback(string prompt)
    {
        var normalizedPrompt = prompt.ToLowerInvariant();
        return normalizedPrompt.Contains("today")
            || normalizedPrompt.Contains("date")
            || normalizedPrompt.Contains("stock")
            || normalizedPrompt.Contains("price");
    }

    private static LLMResponse CreateFallbackResponse(string prompt, IEnumerable<LLMMessage> messages)
    {
        var normalizedPrompt = prompt.ToLowerInvariant();
        var functionMessage = messages.LastOrDefault(message => message.Role == "function");

        if (functionMessage is not null && !string.IsNullOrWhiteSpace(functionMessage.Content))
        {
            try
            {
                using var doc = JsonDocument.Parse(functionMessage.Content);
                if (doc.RootElement.TryGetProperty("currentDateTime", out var dateValue))
                {
                    return new LLMResponse
                    {
                        Text = $"The current date and time is {dateValue.GetString()}",
                        Model = "local-fallback",
                        Provider = "Gemini-Fallback"
                    };
                }

                if (doc.RootElement.TryGetProperty("price", out var priceValue))
                {
                    var symbol = doc.RootElement.TryGetProperty("symbol", out var symbolValue)
                        ? symbolValue.GetString() ?? "UNKNOWN"
                        : "UNKNOWN";
                    var currency = doc.RootElement.TryGetProperty("currency", out var currencyValue)
                        ? currencyValue.GetString() ?? "USD"
                        : "USD";

                    return new LLMResponse
                    {
                        Text = $"{symbol} is currently priced at {priceValue.GetDecimal():0.00} {currency}.",
                        Model = "local-fallback",
                        Provider = "Gemini-Fallback"
                    };
                }
            }
            catch (JsonException)
            {
                // Fall through to the generic fallback.
            }
        }

        if (normalizedPrompt.Contains("today") || normalizedPrompt.Contains("date"))
        {
            return new LLMResponse
            {
                ToolCall = new ToolCall
                {
                    Name = "get_current_date",
                    Args = JsonDocument.Parse("{}").RootElement.Clone()
                },
                Model = "local-fallback",
                Provider = "Gemini-Fallback"
            };
        }

        if (normalizedPrompt.Contains("stock") || normalizedPrompt.Contains("price"))
        {
            var symbol = ExtractTickerSymbol(prompt) ?? "MSFT";

            return new LLMResponse
            {
                ToolCall = new ToolCall
                {
                    Name = "get_stock_price",
                    Args = JsonSerializer.SerializeToElement(new { symbol })
                },
                Model = "local-fallback",
                Provider = "Gemini-Fallback"
            };
        }

        return new LLMResponse
        {
            Text = "Gemini is not configured in this environment, so I’m using a local fallback response. Configure an API key to enable full Gemini responses.",
            Model = "local-fallback",
            Provider = "Gemini-Fallback"
        };
    }

    private static string? ExtractTickerSymbol(string prompt)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SYSTEM",
            "USER",
            "ASSISTANT",
            "FUNCTION",
            "STOCK",
            "PRICE",
            "OF",
            "THE",
            "CURRENT"
        };

        var matches = System.Text.RegularExpressions.Regex.Matches(prompt, @"\b([A-Z]{1,5})\b");
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var symbol = matches[i].Groups[1].Value;
            if (!stopWords.Contains(symbol))
            {
                return symbol;
            }
        }

        return null;
    }

    private static string BuildPrompt(IEnumerable<LLMMessage> messages)
    {
        return string.Join(
            Environment.NewLine,
            messages.Select(FormatMessage).Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string FormatMessage(LLMMessage message)
    {
        return message.Role switch
        {
            "system" => $"System: {message.Content}",
            "user" => $"User: {message.Content}",
            "assistant" => string.IsNullOrWhiteSpace(message.Content)
                ? $"Assistant tool call: {message.Name ?? "unknown"}"
                : $"Assistant: {message.Content}",
            "function" => $"Function {message.Name}: {message.Content}",
            _ => $"{message.Role}: {message.Content}"
        };
    }

    private static GeminiFunctionCall? ExtractFunctionCall(GeminiCandidate? candidate)
    {
        if (candidate?.Content?.FunctionCall is not null)
            return candidate.Content.FunctionCall;

        return candidate?.Content?.Parts?
            .Select(part => part.FunctionCall ?? part.FunctionCallSnakeCase)
            .FirstOrDefault(functionCall => functionCall is not null);
    }

    private static JsonElement NormalizeArguments(JsonElement? args)
    {
        if (args is null || args.Value.ValueKind == JsonValueKind.Undefined)
            return JsonDocument.Parse("{}").RootElement.Clone();

        if (args.Value.ValueKind == JsonValueKind.String)
        {
            var raw = args.Value.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return JsonDocument.Parse("{}").RootElement.Clone();

            try
            {
                return JsonDocument.Parse(raw).RootElement.Clone();
            }
            catch (JsonException)
            {
                return JsonSerializer.SerializeToElement(new { value = raw });
            }
        }

        return args.Value.Clone();
    }

    // --- Gemini wire models (provider-specific) ---
    private sealed record GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; init; }
    }

    private sealed record GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; init; }
    }

    private sealed record GeminiContent
    {
        [JsonPropertyName("parts")]
        public GeminiPart[]? Parts { get; init; }

        [JsonPropertyName("function_call")]
        public GeminiFunctionCall? FunctionCall { get; init; }
    }

    private sealed record GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; init; }

        [JsonPropertyName("functionCall")]
        public GeminiFunctionCall? FunctionCall { get; init; }

        [JsonPropertyName("function_call")]
        public GeminiFunctionCall? FunctionCallSnakeCase { get; init; }
    }

    private sealed record GeminiFunctionCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        // We want the raw JSON for args:
        [JsonPropertyName("arguments")]
        public JsonElement? Args { get; init; }
    }
}