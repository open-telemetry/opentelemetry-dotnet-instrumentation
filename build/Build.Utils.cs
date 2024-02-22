using System.Runtime.InteropServices;
using Nuke.Common.Tools.MSBuild;

partial class Build
{
    private static string AndFilter(params string[] args)
    {
        return string.Join("&", args.Where(s => !string.IsNullOrEmpty(s)));
    }

    private static MSBuildTargetPlatform GetDefaultTargetPlatform()
    {
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            return Arm64TargetPlatform;
        }

        if (RuntimeInformation.OSArchitecture == Architecture.X86)
        {
            return MSBuildTargetPlatform.x86;
        }

        return MSBuildTargetPlatform.x64;
    }

    private static MSBuildTargetPlatform Arm64TargetPlatform = (MSBuildTargetPlatform)"ARM64";
}
