using Logging;
using Models;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Helpers;

internal static class NugetPackageHelper
{
    private static readonly SourceRepository NugetSourceRepository;
    private static readonly NugetConsoleLogger Logger;

    static NugetPackageHelper()
    {
        // TODO: try relay on nuget.config file
        var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        NugetSourceRepository = new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
        Logger = new NugetConsoleLogger();
    }

    public static async Task<NuGetPackageInfo> GetPackageDependenciesAsync(string framework, string packageId, string version)
    {
        var package = await GetPackageMetadataAsync(packageId, version);
        var applicationFramework = NuGetFramework.Parse(framework);
        var packageFramework = GetPackageFramework(applicationFramework, package);
        var dependencies = GetPackageDependencies(packageFramework, package);

        return new()
        {
            Id = packageId,
            ApplicationFramework = applicationFramework,
            PackageFramework = packageFramework,
            Dependencies = dependencies
        };
    }

    public static async Task<IDictionary<NuGetVersion, NuGetPackageInfo>> GetPackageDependenciesAsync(string framework, string packageId, string minVersion, string maxVersion)
    {
        var search = (await GetPackageMetadatasAsync(packageId))
            .Where(x => x.Identity.HasVersion)
            .Where(x => x.Identity.Version >= new NuGetVersion(minVersion))
            .Where(x => x.Identity.Version <= new NuGetVersion(maxVersion));

        var results = new Dictionary<NuGetVersion, NuGetPackageInfo>();
        var applicationFramework = NuGetFramework.Parse(framework);

        foreach (var package in search)
        {
            var packageFramework = GetPackageFramework(applicationFramework, package);
            var dependencies = GetPackageDependencies(packageFramework, package);
            var version = package.Identity.Version;
            var info = new NuGetPackageInfo()
            {
                Id = packageId,
                ApplicationFramework = applicationFramework,
                PackageFramework = packageFramework,
                Dependencies = dependencies
            };

            results.Add(version, info);
        }

        return results;
    }

    private static async Task<IEnumerable<IPackageSearchMetadata>> GetPackageMetadatasAsync(string packageId)
    {
        var packageMetadataResource = await NugetSourceRepository.GetResourceAsync<PackageMetadataResource>();

        return await packageMetadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: false,
            includeUnlisted: true,
            new SourceCacheContext(),
            Logger,
            CancellationToken.None);
    }

    private static async Task<IPackageSearchMetadata> GetPackageMetadataAsync(string packageId, string version)
    {
        var packageMetadataResource = await NugetSourceRepository.GetResourceAsync<PackageMetadataResource>();

        return await packageMetadataResource.GetMetadataAsync(
            new PackageIdentity(packageId, new NuGetVersion(version)),
            new SourceCacheContext(),
            Logger,
            CancellationToken.None);
    }

    private static NuGetFramework GetPackageFramework(NuGetFramework applicationFramework, IPackageSearchMetadata search)
    {
        var packageFramework = search.DependencySets.Any(x => x.TargetFramework == applicationFramework)
            ? applicationFramework
            : GetHighestAvailableNetStandard(search);

        if (packageFramework == null)
        {
            throw new InvalidOperationException("Could not determine package framework.");
        }

        return packageFramework;
    }

    private static NuGetFramework GetHighestAvailableNetStandard(IPackageSearchMetadata search)
    {
        return search.DependencySets
            .Where(x => x.TargetFramework.Framework == ".NETStandard")
            .OrderByDescending(x => x.TargetFramework.Version)
            .Select(x => x.TargetFramework)
            .FirstOrDefault();
    }

    private static IReadOnlyDictionary<string, PackageDependency> GetPackageDependencies(NuGetFramework framework, IPackageSearchMetadata search)
    {
        return search.DependencySets
            .Where(x => x.TargetFramework == framework)
            .SelectMany(x => x.Packages)
            .ToDictionary(k => k.Id, v => v);
    }
}
