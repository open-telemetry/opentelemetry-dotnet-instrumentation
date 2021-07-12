using System;
using System.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

internal static class DotNetSettingsExtensions
{
    public static DotNetBuildSettings SetTargetPlatformAnyCPU(this DotNetBuildSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetPublishSettings SetTargetPlatformAnyCPU(this DotNetPublishSettings settings)
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static T SetTargetPlatformAnyCPU<T>(this T settings)
        where T : MSBuildSettings
        => settings.SetTargetPlatform(MSBuildTargetPlatform.MSIL);

    public static DotNetBuildSettings SetTargetPlatform(this DotNetBuildSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    public static DotNetPublishSettings SetTargetPlatform(this DotNetPublishSettings settings, MSBuildTargetPlatform platform)
    {
        return platform is null
            ? settings
            : settings.SetProperty("Platform", GetTargetPlatform(platform));
    }

    /// <summary>
    /// GitLab installs MSBuild in a non-standard place that causes issues for Nuke trying to resolve it
    /// </summary>
    public static MSBuildSettings SetMSBuildPath(this MSBuildSettings settings)
    {
        var vsRoot = Environment.GetEnvironmentVariable("VSTUDIO_ROOT");

        return settings
            .When(!string.IsNullOrEmpty(vsRoot),
                c => c.SetProcessToolPath(Path.Combine(vsRoot, "MSBuild", "Current", "Bin", "MSBuild.exe")));
    }

    private static string GetTargetPlatform(MSBuildTargetPlatform platform) =>
        platform == MSBuildTargetPlatform.MSIL ? "AnyCPU" : platform.ToString();
}
