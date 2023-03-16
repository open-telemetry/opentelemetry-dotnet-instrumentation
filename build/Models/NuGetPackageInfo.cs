using NuGet.Frameworks;
using NuGet.Packaging.Core;

namespace Models;

internal class NuGetPackageInfo
{
    public string Id { get; init; }
    public NuGetFramework ApplicationFramework { get; init; }
    public NuGetFramework PackageFramework { get; init; }
    public IReadOnlyDictionary<string, PackageDependency> Dependencies { get; init; }
}
