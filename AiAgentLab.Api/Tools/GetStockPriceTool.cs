using System.Text.Json;

namespace AiAgentLab.Api.Tools
{
    public sealed class GetStockPriceTool : ITool
    {
        public string Name => "get_stock_price";
        public string Description => "Gets a mock current stock price for a stock ticker symbol.";

        private static readonly Dictionary<string, decimal> Prices = new()
        {
            ["AAPL"] = 189.50m,
            ["MSFT"] = 420.25m,
            ["GOOGL"] = 175.10m,
            ["TSLA"] = 180.00m,
            ["NVDA"] = 125.50m,
            ["AMZN"] = 185.75m,
            ["META"] = 510.40m
        };

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

        public Task<JsonElement> ExecuteAsync(JsonElement args, CancellationToken cancellationToken)
        {
            if (!args.TryGetProperty("symbol", out var symEl) || symEl.ValueKind != JsonValueKind.String)
            {
                var err = new { error = "Invalid or missing 'symbol' parameter." };
                return Task.FromResult(JsonSerializer.SerializeToElement(err));
            }

            var symbol = symEl.GetString()!.ToUpperInvariant();

            if (!Prices.TryGetValue(symbol, out var price))
            {
                var notFound = new
                {
                    symbol,
                    found = false,
                    message = $"No mock price found for this symbol."
                };
                return Task.FromResult(JsonSerializer.SerializeToElement(notFound));
            }

            var found = new
            {
                symbol,
                found = true,
                price = price,
                currency = "USD"
            };
            return Task.FromResult(JsonSerializer.SerializeToElement(found));
        }
    }
}