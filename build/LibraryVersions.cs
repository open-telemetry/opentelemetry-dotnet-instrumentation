using Models;
using Nuke.Common.Tools.MSBuild;

public static partial class LibraryVersion
{
    public static bool TryGetVersions(string applicationName, MSBuildTargetPlatform platform, out IReadOnlyCollection<PackageBuildInfo> libraryVersions)
    {
        var result = Versions.TryGetValue(applicationName, out libraryVersions);
        if (result)
        {
            libraryVersions = libraryVersions
                .Where(x =>
                    x.SupportedPlatforms.Contains(platform.ToString(), StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return result;
    }
}
