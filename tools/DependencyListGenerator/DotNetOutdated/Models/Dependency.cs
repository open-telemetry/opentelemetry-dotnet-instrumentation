using NuGet.Versioning;

namespace DependencyListGenerator.DotNetOutdated.Models;

public class Dependency
{
    public Dependency(string name, VersionRange versionRange, NuGetVersion resolvedVersion, bool isAutoReferenced, bool isTransitive, bool isDevelopmentDependency, bool isVersionCentrallyManaged)
    {
        Name = name;
        VersionRange = versionRange;
        ResolvedVersion = resolvedVersion;
        IsAutoReferenced = isAutoReferenced;
        IsTransitive = isTransitive;
        IsDevelopmentDependency = isDevelopmentDependency;
        IsVersionCentrallyManaged = isVersionCentrallyManaged;
    }

    public bool IsAutoReferenced { get; }

    public bool IsDevelopmentDependency { get; }

    public bool IsTransitive { get; }

    public bool IsVersionCentrallyManaged { get; }

    public string Name { get; }

    public NuGetVersion ResolvedVersion { get; }

    public VersionRange VersionRange { get; }
}
