namespace AiAgentLab.Api.Core.Configuration;

/// <summary>SQL Server connection configuration.</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";

    /// <summary>SQL Server connection string.</summary>
    public required string ConnectionString { get; init; }
}