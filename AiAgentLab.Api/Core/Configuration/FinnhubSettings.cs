namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// Configuration for the Finnhub market-data API (https://finnhub.io).
/// Bound from the "Finnhub" section.
/// </summary>
public sealed class FinnhubSettings
{
    public const string SectionName = "Finnhub";

    /// <summary>Base URL for the Finnhub REST API.</summary>
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";

    /// <summary>API key for authenticating requests (use user-secrets or env in production; never hardcode).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Request timeout in seconds for Finnhub calls.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}
