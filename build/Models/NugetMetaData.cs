using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace Models;

internal class PackageDependencySets : Dictionary<NuGetFramework, Dictionary<string, PackageDependency>>
{
    public PackageDependencySets(IEnumerable<PackageDependencyGroup> dependencySets)
        : base(dependencySets.ToDictionary(
            k => k.TargetFramework,
            v => new Dictionary<string, PackageDependency>(
                    v.Packages.ToDictionary(dk => dk.Id, dv => dv))))
    {
    }
}
