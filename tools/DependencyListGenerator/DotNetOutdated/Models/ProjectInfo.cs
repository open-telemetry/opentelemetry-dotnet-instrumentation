using System.Text.Json.Serialization;

namespace DependencyListGenerator.DotNetOutdated.Models;

public record ProjectInfo
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("frameworks")]
    public IReadOnlyCollection<FrameworkInfo> Frameworks { get; init; } = [];
}
