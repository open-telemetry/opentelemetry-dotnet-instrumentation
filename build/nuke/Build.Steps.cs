using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using static DotNetMSBuildTasks;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Solution("OpenTelemetry.AutoInstrumentation.sln")] readonly Solution Solution;
    AbsolutePath MsBuildProject => RootDirectory / "OpenTelemetry.AutoInstrumentation.proj";

    AbsolutePath OutputDirectory => RootDirectory / "bin";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "test";

    AbsolutePath TracerHomeDirectory => TracerHome ?? (OutputDirectory / "tracer-home");
    AbsolutePath ArtifactsDirectory => Artifacts ?? (OutputDirectory / "artifacts");
    AbsolutePath BuildDataDirectory => RootDirectory / "build_data";

    Project NativeProfilerProject => Solution.GetProject(Projects.ClrProfilerNative);

    [LazyPathExecutable(name: "cmd")] readonly Lazy<Tool> Cmd;
    [LazyPathExecutable(name: "cmake")] readonly Lazy<Tool> CMake;
    [LazyPathExecutable(name: "make")] readonly Lazy<Tool> Make;

    IEnumerable<MSBuildTargetPlatform> ArchitecturesForPlatform =>
        Equals(Platform, MSBuildTargetPlatform.x64)
            ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
            : new[] { MSBuildTargetPlatform.x86 };

    readonly IEnumerable<TargetFramework> TargetFrameworks = new[]
    {
        TargetFramework.NET461,
        TargetFramework.NETSTANDARD2_0,
        TargetFramework.NETCOREAPP3_1,
    };

    Target CreateRequiredDirectories => _ => _
        .Unlisted()
        .Executes(() =>
        {
            EnsureExistingDirectory(TracerHomeDirectory);
            EnsureExistingDirectory(ArtifactsDirectory);
            EnsureExistingDirectory(BuildDataDirectory);
        });

    Target Restore => _ => _
        .After(Clean)
        .Unlisted()
        .Executes(() =>
        {
            if (IsWin)
            {
                NuGetTasks.NuGetRestore(s => s
                    .SetTargetPath(Solution)
                    .SetVerbosity(NuGetVerbosity.Normal)
                    .When(!string.IsNullOrEmpty(NugetPackageDirectory), o =>
                        o.SetPackagesDirectory(NugetPackageDirectory)));
            }
            else
            {
                DotNetRestore(s => s
                    .SetProjectFile(Solution)
                    .SetVerbosity(DotNetVerbosity.Normal)
                    // .SetTargetPlatform(Platform) // necessary to ensure we restore every project
                    .SetProperty("configuration", BuildConfiguration.ToString())
                    .When(!string.IsNullOrEmpty(NugetPackageDirectory), o =>
                        o.SetPackageDirectory(NugetPackageDirectory)));
            }
        });

    Target CompileManagedSrc => _ => _
        .Unlisted()
        .Description("Compiles the managed code in the src directory")
        .After(CreateRequiredDirectories)
        .After(Restore)
        .Executes(() =>
        {
            // Always AnyCPU
            DotNetMSBuild(x => x
                .SetTargetPath(MsBuildProject)
                .SetTargetPlatformAnyCPU()
                .SetConfiguration(BuildConfiguration)
                .DisableRestore()
                .SetTargets("BuildCsharp")
            );
        });

    Target CompileNativeSrc => _ => _
        .Unlisted()
        .Description("Compiles the native loader")
        .DependsOn(CompileNativeSrcWindows)
        .DependsOn(CompileNativeSrcLinux)
        .DependsOn(CompileNativeSrcMacOs);


    Target CompileNativeTests => _ => _
        .Unlisted()
        .Description("Compiles the native loader unit tests")
        .DependsOn(CompileNativeTestsWindows)
        .DependsOn(CompileNativeTestsLinux);

    Target PublishManagedProfiler => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .Executes(() =>
        {
            var targetFrameworks = IsWin
                ? TargetFrameworks
                : TargetFrameworks.Where(framework => !framework.ToString().StartsWith("net4"));

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.ClrProfilerManaged))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TracerHomeDirectory / framework)));
        });

    Target PublishNativeProfiler => _ => _
        .Unlisted()
        .DependsOn(PublishNativeProfilerWindows)
        .DependsOn(PublishNativeProfilerLinux)
        .DependsOn(PublishNativeProfilerMacOs);

    Target CopyIntegrationsJson => _ => _
        .Unlisted()
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var source = RootDirectory / "integrations.json";
            var dest = TracerHomeDirectory;

            Logger.Info($"Copying '{source}' to '{dest}'");
            CopyFileToDirectory(source, dest, FileExistsPolicy.Overwrite);
        });

    Target RunNativeTests => _ => _
        .Unlisted()
        .DependsOn(RunNativeTestsWindows)
        .DependsOn(RunNativeTestsLinux);
}
