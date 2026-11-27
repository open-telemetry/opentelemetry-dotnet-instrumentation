using Fallout.Common;
using Fallout.Common.IO;
using Fallout.Common.Tooling;
using static Fallout.Common.EnvironmentInfo;

partial class Build
{
    AbsolutePath NativeTestsBuildDirectory => NativeProfilerProject.Directory / "build" / "tests";

    Target CompileNativeTestsUnix => _ => _
        .Unlisted()
        .After(CreateRequiredDirectories)
        .OnlyWhenStatic(() => IsLinux || IsOsx)
        .Executes(() =>
        {
            var buildDirectory = NativeTestsBuildDirectory;
            buildDirectory.CreateDirectory();

            var (major, minor, patch) = VersionHelper.GetVersionParts();
            CMake.Value(
                arguments: $"-S {NativeProfilerProject.Directory} -DCMAKE_BUILD_TYPE=Release -DOTEL_BUILD_NATIVE_TESTS=ON -DOTEL_AUTO_VERSION={VersionHelper.GetVersionWithoutSuffixes()} -DOTEL_AUTO_VERSION_MAJOR={major} -DOTEL_AUTO_VERSION_MINOR={minor} -DOTEL_AUTO_VERSION_PATCH={patch}",
                workingDirectory: buildDirectory);
            CMake.Value(
                arguments: $"--build . --parallel {Environment.ProcessorCount} --target {Projects.Tests.AutoInstrumentationNativeTests}",
                workingDirectory: buildDirectory);
        });

    Target RunNativeTestsUnix => _ => _
        .Unlisted()
        .After(CompileNativeTestsUnix, PublishManagedProfiler)
        .OnlyWhenStatic(() => IsLinux || IsOsx)
        .Executes(() =>
        {
            var workingDirectory = NativeTestsBuildDirectory / "bin";
            var testExe = ToolResolver.GetTool(workingDirectory / Projects.Tests.AutoInstrumentationNativeTests);
            var envVars = EnvironmentInfo.Variables.ToDictionary(x => x.Key, x => x.Value);
            envVars["OTEL_DOTNET_AUTO_LOG_DIRECTORY"] = ProfilerTestLogs;

            testExe(string.Empty, workingDirectory: workingDirectory, environmentVariables: envVars);
        });
}
