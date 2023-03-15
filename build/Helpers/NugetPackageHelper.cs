using Logging;
using Models;
using NuGet.Configuration;
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

    public static async Task<PackageDependencySets> GetPackageDependenciesAsync(string packageId, string version)
    {
        var package = await GetPackageMetadataAsync(packageId, version);

        return new PackageDependencySets(package.DependencySets);
    }

    public static async Task<IEnumerable<(NuGetVersion, PackageDependencySets)>> GetPackageDependenciesAsync(string packageId, string minVersion, string maxVersion)
    {
        var search = (await GetPackageMetadatasAsync(packageId))
            .Where(x => x.Identity.HasVersion)
            .Where(x => x.Identity.Version >= new NuGetVersion(minVersion))
            .Where(x => x.Identity.Version <= new NuGetVersion(maxVersion));

        var results = new List<(NuGetVersion, PackageDependencySets)>();

        foreach (var package in search)
        {
            results.Add((
                package.Identity.Version,
                new PackageDependencySets(package.DependencySets)
            ));
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
}
