using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiAgentLab.Api.Core.Configuration;
using Microsoft.Extensions.Options;

namespace AiAgentLab.Api.Tools
{
    /// <summary>
    /// Live stock-quote tool backed by the Finnhub REST API (https://finnhub.io).
    /// Unlike <see cref="GetStockPriceTool"/> (a static mock), this calls a real
    /// online API. It uses a typed HttpClient and strongly typed settings, mirroring
    /// the GeminiLLMProvider pattern.
    /// </summary>
    public sealed class GetStockPriceFromFinnhubTool : ITool
    {
        private readonly HttpClient _httpClient;
        private readonly FinnhubSettings _settings;
        private readonly ILogger<GetStockPriceFromFinnhubTool> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GetStockPriceFromFinnhubTool(
            HttpClient httpClient,
            IOptions<FinnhubSettings> settings,
            ILogger<GetStockPriceFromFinnhubTool> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public string Name => "get_live_stock_price";

        public string Description =>
            "Gets the live/current market price for a stock ticker symbol using the Finnhub API.";

        public object GetDeclaration()
        {
            return new
            {
                name = Name,
                description = Description,
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        symbol = new { type = "string", description = "Ticker symbol, e.g. AAPL" }
                    },
                    required = new[] { "symbol" }
                }
            };
        }

        public async Task<JsonElement> ExecuteAsync(JsonElement args, CancellationToken cancellationToken)
        {
            if (!args.TryGetProperty("symbol", out var symEl) || symEl.ValueKind != JsonValueKind.String)
            {
                return JsonSerializer.SerializeToElement(new { error = "Invalid or missing 'symbol' parameter." });
            }

            var symbol = symEl.GetString()!.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return JsonSerializer.SerializeToElement(new { error = "Invalid or missing 'symbol' parameter." });
            }

            // Without an API key we can't talk to Finnhub, so return a clear, LLM-friendly error.
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogWarning("Finnhub API key is not configured; cannot fetch a live price for {Symbol}.", symbol);
                return JsonSerializer.SerializeToElement(new
                {
                    symbol,
                    found = false,
                    error = "finnhub_not_configured",
                    message = "Finnhub API key is not configured. Set the Finnhub:ApiKey secret to enable live prices."
                });
            }

            // Finnhub quote endpoint: /quote?symbol=AAPL&token=API_KEY
            var url = $"quote?symbol={Uri.EscapeDataString(symbol)}&token={Uri.EscapeDataString(_settings.ApiKey)}";

            _logger.LogInformation("Requesting live quote from Finnhub for {Symbol}", symbol);

            try
            {
                var quote = await _httpClient.GetFromJsonAsync<FinnhubQuote>(url, _jsonOptions, cancellationToken);

                // Finnhub returns all-zero fields for an unknown symbol rather than an error status.
                if (quote is null || quote.Current == 0m)
                {
                    return JsonSerializer.SerializeToElement(new
                    {
                        symbol,
                        found = false,
                        message = "No live price found for this symbol."
                    });
                }

                return JsonSerializer.SerializeToElement(new
                {
                    symbol,
                    found = true,
                    price = quote.Current,
                    change = quote.Change,
                    percentChange = quote.PercentChange,
                    high = quote.High,
                    low = quote.Low,
                    open = quote.Open,
                    previousClose = quote.PreviousClose,
                    currency = "USD"
                });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Finnhub request failed for {Symbol}.", symbol);
                return JsonSerializer.SerializeToElement(new
                {
                    symbol,
                    found = false,
                    error = "finnhub_request_failed",
                    message = ex.Message
                });
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Finnhub request timed out for {Symbol}.", symbol);
                return JsonSerializer.SerializeToElement(new
                {
                    symbol,
                    found = false,
                    error = "finnhub_timeout",
                    message = "The request to Finnhub timed out."
                });
            }
        }

        // --- Finnhub wire model (provider-specific) ---
        // See https://finnhub.io/docs/api/quote
        private sealed record FinnhubQuote
        {
            [JsonPropertyName("c")]
            public decimal Current { get; init; }

            [JsonPropertyName("d")]
            public decimal? Change { get; init; }

            [JsonPropertyName("dp")]
            public decimal? PercentChange { get; init; }

            [JsonPropertyName("h")]
            public decimal High { get; init; }

            [JsonPropertyName("l")]
            public decimal Low { get; init; }

            [JsonPropertyName("o")]
            public decimal Open { get; init; }

            [JsonPropertyName("pc")]
            public decimal PreviousClose { get; init; }
        }
    }
}
