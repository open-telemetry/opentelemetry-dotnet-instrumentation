namespace DependencyListGenerator.DotNetOutdated.Models;

public class Dependency(string name, string resolvedVersion, bool isDevelopmentDependency)
{
    public bool IsDevelopmentDependency { get; } = isDevelopmentDependency;

    public string Name { get; } = name;

    public string ResolvedVersion { get; } = resolvedVersion;
}
