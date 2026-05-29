namespace AiAgentLab.Api.Models.Health;

/// <summary>Response body for <c>GET /api/health</c>.</summary>
public sealed record HealthResponse
{
    public required string Status { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public required DateTimeOffset Timestamp { get; init; }
}
