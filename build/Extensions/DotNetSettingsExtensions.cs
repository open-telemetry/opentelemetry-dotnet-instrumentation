using Models;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

namespace Extensions;

internal static class DotNetSettingsExtensions
{
    public static DotNetPublishSettings SetTargetPlatformAnyCPU(this DotNetPublishSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetTestSettings SetTargetPlatformAnyCPU(this DotNetTestSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetMSBuildSettings SetTargetPlatformAnyCPU(this DotNetMSBuildSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetPublishSettings SetTargetPlatform(this DotNetPublishSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    public static DotNetTestSettings SetTargetPlatform(this DotNetTestSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    public static DotNetMSBuildSettings SetTargetPlatform(this DotNetMSBuildSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    public static DotNetTestSettings EnableTrxLogOutput(this DotNetTestSettings settings, string resultsDirectory)
    {
        return settings
            .AddLoggers("trx")
            .SetResultsDirectory(resultsDirectory);
    }

    public static DotNetMSBuildSettings EnableTrxLogOutput(this DotNetMSBuildSettings settings, string resultsDirectory)
    {
        return settings
            .SetProperty("VSTestLogger", "trx")
            .SetProperty("VSTestResultsDirectory", resultsDirectory);
    }

    public static DotNetMSBuildSettings SetBlameHangTimeout(this DotNetMSBuildSettings settings, string timeout)
    {
        return settings
            .SetProperty("VSTestBlameHang", true)
            .SetProperty("VSTestBlameHangTimeout", timeout);
    }

    public static DotNetMSBuildSettings RunTests(this DotNetMSBuildSettings settings)
    {
        return settings
            .SetTargets("VSTest")
            .SetProperty("VSTestNoBuild", true);
    }

    public static DotNetMSBuildSettings SetFilter(this DotNetMSBuildSettings settings, string filter)
    {
        return settings
            .SetProperty("VSTestTestCaseFilter", filter);
    }

    public static DotNetBuildSettings[] CombineWithBuildInfos(this DotNetBuildSettings settings, IReadOnlyCollection<PackageBuildInfo> buildInfos, TargetFramework targetFramework)
    {
        // NOTE: SetProperty creates internally a new instance!
#if NET7_0
        // workaround for building on Centos. It should be removed when we drop support for .NET6/.NET7. ETA November 2024
        return settings.CombineWith(buildInfos.Where(buildInfo => (targetFramework == TargetFramework.NOT_SPECIFIED || buildInfo.SupportedFrameworks.Length == 0 || buildInfo.SupportedFrameworks.Contains(targetFramework)) && !buildInfo.SupportedFrameworks.Contains(TargetFramework.NET8_0)), (p, buildInfo) =>
#else
        return settings.CombineWith(buildInfos.Where(buildInfo => targetFramework == TargetFramework.NOT_SPECIFIED || buildInfo.SupportedFrameworks.Length == 0 || buildInfo.SupportedFrameworks.Contains(targetFramework)), (p, buildInfo) =>
#endif
        {
            p = p.SetProperty("LibraryVersion", buildInfo.LibraryVersion);

            foreach (var item in buildInfo.AdditionalMetaData)
            {
                p = p.SetProperty(item.Key, item.Value);
            }

            if (buildInfo.SupportedFrameworks.Length > 0)
            {
                p = p.SetProperty("TargetFrameworks", string.Join(";", buildInfo.SupportedFrameworks));
            }

            return p;
        });
    }

    public static DotNetRestoreSettings[] CombineWithBuildInfos(this DotNetRestoreSettings settings, IReadOnlyCollection<PackageBuildInfo> buildInfos)
    {
        // NOTE: SetProperty creates internally a new instance!
#if NET7_0
        // workaround for building on Centos. It should be removed when we drop support for .NET6/.NET7. ETA November 2024
        return settings.CombineWith(buildInfos.Where(buildInfo => !buildInfo.SupportedFrameworks.Contains(TargetFramework.NET8_0)), (p, buildInfo) =>
#else
        return settings.CombineWith(buildInfos, (p, buildInfo) =>
#endif
        {
            p = p.SetProperty("LibraryVersion", buildInfo.LibraryVersion);

            foreach (var item in buildInfo.AdditionalMetaData)
            {
                p = p.SetProperty(item.Key, item.Value);
            }

            if (buildInfo.SupportedFrameworks.Length > 0)
            {
                p = p.SetProperty("TargetFrameworks", string.Join(";", buildInfo.SupportedFrameworks));
            }

            return p;
        });
    }

    private static string GetTargetPlatform(MSBuildTargetPlatform platform) =>
        platform == MSBuildTargetPlatform.MSIL ? "AnyCPU" : platform.ToString();
}
