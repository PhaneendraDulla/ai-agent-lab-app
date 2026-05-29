namespace AiAgentLab.Api.Core.Configuration;

/// <summary>
/// General application metadata. Bound from the "App" section and surfaced
/// by the health endpoint.
/// </summary>
public sealed class AppSettings
{
    public const string SectionName = "App";

    public string Name { get; set; } = "AiAgentLab.Api";

    public string Version { get; set; } = "0.1.0";
}
