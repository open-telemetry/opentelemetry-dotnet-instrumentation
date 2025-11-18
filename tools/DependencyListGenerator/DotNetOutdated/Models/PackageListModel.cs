namespace DependencyListGenerator.DotNetOutdated.Models;

using System.Text.Json.Serialization;

public record PackageListModel
{
    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("parameters")]
    public string Parameters { get; init; } = string.Empty;

    [JsonPropertyName("projects")]
    public ProjectInfo[] Projects { get; init; } = [];

    public record ProjectInfo
    {
        [JsonPropertyName("path")]
        public string Path { get; init; } = string.Empty;

        [JsonPropertyName("frameworks")]
        public FrameworkInfo[] Frameworks { get; init; } = [];
    }

    public record FrameworkInfo
    {
        [JsonPropertyName("framework")]
        public string Framework { get; init; } = string.Empty;

        [JsonPropertyName("topLevelPackages")]
        public PackageReference[] TopLevelPackages { get; init; } = [];

        [JsonPropertyName("transitivePackages")]
        public PackageReference[] TransitivePackages { get; init; } = [];
    }

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
}
