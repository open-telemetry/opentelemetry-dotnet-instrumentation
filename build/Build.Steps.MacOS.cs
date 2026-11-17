using Fallout.Common;
using Fallout.Common.IO;
using Serilog;
using static Fallout.Common.EnvironmentInfo;

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
            foreach (var project in Solution.GetNativeDependenciesForManagedTests())
            {
                var buildDirectory = project.Directory.ToString();
                CMake.Value(
                    arguments: "-S .",
                    workingDirectory: buildDirectory);
                Make.Value(
                    arguments: $"",
                    workingDirectory: buildDirectory);
            }
        });

    Target PublishNativeProfilerMacOs => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsOsx)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            // Create home directory
            var source = NativeProfilerProject.Directory / "build" / "bin" / $"{NativeProfilerProject.Name}.dylib";
            var platform = Platform.ToString().ToLowerInvariant();
            var dest = TracerHomeDirectory / $"osx-{platform}";
            Log.Information($"Copying '{source}' to '{dest}'");

            source.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
        });
}
