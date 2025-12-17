using System.Text.Json.Serialization;

namespace DependencyListGenerator.DotNetOutdated.Models;

public record FrameworkInfo
{
    [JsonPropertyName("framework")]
    public string Framework { get; init; } = string.Empty;

    [JsonPropertyName("topLevelPackages")]
    public IReadOnlyCollection<PackageReference> TopLevelPackages { get; init; } = [];

    [JsonPropertyName("transitivePackages")]
    public IReadOnlyCollection<PackageReference> TransitivePackages { get; init; } = [];
}
