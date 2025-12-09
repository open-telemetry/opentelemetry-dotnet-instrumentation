using System.Text.Json.Serialization;

namespace DependencyListGenerator.DotNetOutdated.Models;

public record PackageReference
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("requestedVersion")]
    public string RequestedVersion { get; init; }

    [JsonPropertyName("resolvedVersion")]
    public string ResolvedVersion { get; init; } = string.Empty;

    [JsonPropertyName("latestVersion")]
    public string LatestVersion { get; init; }
}
