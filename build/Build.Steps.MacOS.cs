using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using static Nuke.Common.EnvironmentInfo;

partial class Build
{
    Target CompileNativeSrcMacOs => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsOsx)
        .Executes(() =>
        {
            var buildDirectory = NativeProfilerProject.Directory / "build";
            buildDirectory.CreateDirectory();

            var (major, minor, patch) = VersionHelper.GetVersionParts();

            CMake.Value(
                arguments: $"../ -DCMAKE_BUILD_TYPE=Release -DOTEL_AUTO_VERSION={VersionHelper.GetVersionWithoutSuffixes()} -DOTEL_AUTO_VERSION_MAJOR={major} -DOTEL_AUTO_VERSION_MINOR={minor} -DOTEL_AUTO_VERSION_PATCH={patch}",
                workingDirectory: buildDirectory);
            Make.Value(
                arguments: $"",
                workingDirectory: buildDirectory);
        });

    Target CompileNativeDependenciesForManagedTestsMacOs => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsOsx)
        .Executes(() =>
        {
            var buildDirectory = Solution.GetContinuousProfilerNativeDep().Directory.ToString();
            CMake.Value(
                arguments: "-S .",
                workingDirectory: buildDirectory);
            Make.Value(
                arguments: $"",
                workingDirectory: buildDirectory);
        });

    Target PublishNativeProfilerMacOs => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsOsx)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            // Create home directory
            var source = NativeProfilerProject.Directory / "build" / "bin";
            var sourceLib = source / $"{NativeProfilerProject.Name}.dylib";
            var sourceSymbols = source / $"{NativeProfilerProject.Name}.dSYM";
            var platform = Platform.ToString().ToLowerInvariant();
            var dest = TracerHomeDirectory / $"osx-{platform}";

            Log.Information($"Copying '{sourceLib}' to '{dest}'");
            sourceLib.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);

            Log.Information($"Copying '{sourceSymbols}' to '{dest}'");
            sourceSymbols.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
        });
}
