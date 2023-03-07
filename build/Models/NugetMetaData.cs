using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace Models;

internal class NugetMetaData : Dictionary<NuGetFramework, Dictionary<string, PackageDependency>>
{
    public NugetMetaData(IEnumerable<PackageDependencyGroup> dependencySets)
        : base(dependencySets.ToDictionary(
            k => k.TargetFramework,
            v => new Dictionary<string, PackageDependency>(
                    v.Packages.ToDictionary(dk => dk.Id, dv => dv))))
    {
    }
}
