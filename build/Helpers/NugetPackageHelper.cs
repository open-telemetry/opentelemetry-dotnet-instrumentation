using Logging;
using Models;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Helpers;

internal static class NugetPackageHelper
{
    public static async Task<NugetMetaData> GetPackageDependenciesAsync(string packageId, string version)
    {
        var package = await GetPackageMetadataAsync(packageId, version);

        return new NugetMetaData(package.DependencySets);
    }

    public static async Task<IEnumerable<(NuGetVersion, NugetMetaData)>> GetPackageDependenciesAsync(string packageId, string minVersion, string maxVersion)
    {
        var search = (await GetPackageMetadatasAsync(packageId))
            .Where(x => x.Identity.HasVersion)
            .Where(x => x.Identity.Version >= new NuGetVersion(minVersion))
            .Where(x => x.Identity.Version <= new NuGetVersion(maxVersion));

        var results = new List<(NuGetVersion, NugetMetaData)>();

        foreach (var package in search)
        {
            results.Add((
                package.Identity.Version,
                new NugetMetaData(package.DependencySets)
            ));
        }

        return results;
    }

    private static async Task<IEnumerable<IPackageSearchMetadata>> GetPackageMetadatasAsync(string packageId)
    {
        var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        var sourceRepository = new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
        var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

        return await packageMetadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: false,
            includeUnlisted: true,
            new SourceCacheContext(),
            new ConsoleLogger(),
            CancellationToken.None);
    }

    private static async Task<IPackageSearchMetadata> GetPackageMetadataAsync(string packageId, string version)
    {
        var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        var sourceRepository = new SourceRepository(packageSource, Repository.Provider.GetCoreV3());
        var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

        return await packageMetadataResource.GetMetadataAsync(
            new PackageIdentity(packageId, new NuGetVersion(version)),
            new SourceCacheContext(),
            new ConsoleLogger(),
            CancellationToken.None);
    }
}
