using Fallout.Common;
using Fallout.Common.IO;
using Serilog;
using static Fallout.Common.EnvironmentInfo;

partial class Build
{
    Target CompileNativeSrcLinux => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsLinux)
        .Executes(() =>
        {
            var buildDirectory = NativeProfilerProject.Directory / "build";
            buildDirectory.CreateDirectory();

            var (major, minor, patch) = VersionHelper.GetVersionParts();

            CMake.Value(
                arguments: $"../ -DCMAKE_CXX_COMPILER=clang++ -DCMAKE_C_COMPILER=clang -DCMAKE_BUILD_TYPE=Release -DOTEL_AUTO_VERSION={VersionHelper.GetVersionWithoutSuffixes()} -DOTEL_AUTO_VERSION_MAJOR={major} -DOTEL_AUTO_VERSION_MINOR={minor} -DOTEL_AUTO_VERSION_PATCH={patch}",
                workingDirectory: buildDirectory);
            CMake.Value(
                arguments: "--build . --parallel",
                workingDirectory: buildDirectory);
        });

    Target CompileNativeDependenciesForManagedTestsLinux => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsLinux)
        .Executes(() =>
        {
            var buildDirectory = Solution.GetContinuousProfilerNativeDep().Directory.ToString();
            CMake.Value(
                arguments: "-DCMAKE_CXX_COMPILER=clang++ -DCMAKE_C_COMPILER=clang -S .",
                workingDirectory: buildDirectory);
            CMake.Value(
                arguments: "--build . --parallel",
                workingDirectory: buildDirectory);
        });

    Target PublishNativeProfilerLinux => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsLinux)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            // Copy Native files
            var source = NativeProfilerProject.Directory / "build" / "bin";
            var sourceLib = source / $"{NativeProfilerProject.Name}.so";
            var sourceDebug = source / $"{NativeProfilerProject.Name}.so.debug";
            var platform = Platform.ToString().ToLowerInvariant();
            var clrProfilerDirectoryName = Environment.GetEnvironmentVariable("OS_TYPE") switch
            {
                "linux-musl" => $"linux-musl-{platform}",
                _ => $"linux-{platform}"
            };

            var dest = TracerHomeDirectory / clrProfilerDirectoryName;

            Log.Information($"Copying '{sourceLib}' to '{dest}'");
            sourceLib.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);

            Log.Information($"Copying '{sourceDebug}' to '{dest}'");
            sourceDebug.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
        });
}
