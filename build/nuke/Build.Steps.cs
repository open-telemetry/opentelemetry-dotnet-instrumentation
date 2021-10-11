using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
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
    [Solution("OpenTelemetry.ClrProfiler.sln")] readonly Solution Solution;
    AbsolutePath MsBuildProject => RootDirectory / "OpenTelemetry.ClrProfiler.proj";

    AbsolutePath OutputDirectory => RootDirectory / "bin";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "test";

    AbsolutePath TracerHomeDirectory => TracerHome ?? (OutputDirectory / "tracer-home");
    AbsolutePath ArtifactsDirectory => Artifacts ?? (OutputDirectory / "artifacts");
    AbsolutePath BuildDataDirectory => RootDirectory / "build_data";
    AbsolutePath ProfilerTestLogs => BuildDataDirectory / "profiler-logs";

    Project NativeProfilerProject => Solution.GetProject(Projects.ClrProfilerNative);

    [LazyPathExecutable(name: "cmd")] readonly Lazy<Tool> Cmd;
    [LazyPathExecutable(name: "cmake")] readonly Lazy<Tool> CMake;
    [LazyPathExecutable(name: "make")] readonly Lazy<Tool> Make;

    IEnumerable<MSBuildTargetPlatform> ArchitecturesForPlatform =>
        Equals(Platform, MSBuildTargetPlatform.x64)
            ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
            : new[] { MSBuildTargetPlatform.x86 };

    private static readonly IEnumerable<TargetFramework> TargetFrameworks = new[]
    {
        TargetFramework.NET461,
        TargetFramework.NETCOREAPP3_1
    };

    private static readonly IEnumerable<TargetFramework> TestFrameworks = TargetFrameworks
        .Concat(new[] {
            TargetFramework.NET5_0
        });

    Target CreateRequiredDirectories => _ => _
        .Unlisted()
        .Executes(() =>
        {
            EnsureExistingDirectory(TracerHomeDirectory);
            EnsureExistingDirectory(ArtifactsDirectory);
            EnsureExistingDirectory(BuildDataDirectory);
            EnsureExistingDirectory(ProfilerTestLogs);
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

    Target CompileManagedTests => _ => _
        .Unlisted()
        .Description("Compiles the managed code in the test directory")
        .After(CompileManagedSrc)
        .Executes(() =>
        {
            // Always AnyCPU
            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Tests.ClrProfilerManagedLoaderTests))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(true));

            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Tests.ClrProfilerManagedBootstrappingTests))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(true));

            DotNetMSBuild(x => x
                .SetTargetPath(MsBuildProject)
                .SetTargetPlatform(Platform)
                .SetConfiguration(BuildConfiguration)
                .DisableRestore()
                .SetTargets("BuildCsharpTest"));
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

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.DotnetStartupHook))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .SetFramework(TargetFramework.NETCOREAPP3_1)
                .SetOutput(TracerHomeDirectory / TargetFramework.NETCOREAPP3_1));
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

    Target RunManagedTests => _ => _
        .Unlisted()
        .Produces(BuildDataDirectory / "profiler-logs" / "*")
        .After(BuildTracer)
        .After(CompileManagedTests)
        .After(PublishMocks)
        .Executes(() =>
        {
            RunUnitTests();
            RunIntegrationTests();
        });

    Target PublishMocks => _ => _
        .Unlisted()
        .After(CompileMocks)
        .After(CompileManagedTests)
        .Executes(() =>
        {
            // publish ClrProfilerManaged moc
            var targetFrameworks = IsWin
                ? TargetFrameworks
                : TargetFrameworks.ExceptNetFramework();

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.Mocks.ClrProfilerManagedMock))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TestsDirectory / Projects.Tests.ClrProfilerManagedLoaderTests / "bin" / BuildConfiguration / "Profiler" / framework)));
        });

    Target CompileMocks => _ => _
        .Unlisted()
        .Executes(() =>
        {
            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Mocks.ClrProfilerManagedMock))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(true)
            );
        });

    private AbsolutePath GetResultsDirectory(Project proj) => BuildDataDirectory / "results" / proj.Name;

    private void RunUnitTests()
    {
        RunBootstrappingTests();

        var unitTestProjects = new[]
        {
            Solution.GetProject(Projects.Tests.ClrProfilerManagedLoaderTests)
        };

        DotNetTest(config => config
            .SetConfiguration(BuildConfiguration)
            .SetTargetPlatformAnyCPU()
            .EnableNoRestore()
            .EnableNoBuild()
            .CombineWith(unitTestProjects, (s, project) => s
                .EnableTrxLogOutput(GetResultsDirectory(project))
                .SetProjectFile(project)), degreeOfParallelism: 4);
    }

    /// <summary>
    /// Bootstrapping tests require every single test to be run in a separate process
    /// so the tracer can be created from scratch for each of them.
    /// </summary>
    private void RunBootstrappingTests()
    {
        var project = Solution.GetProject(Projects.Tests.ClrProfilerManagedBootstrappingTests);

        const string testPrefix = "OpenTelemetry.ClrProfiler.Managed.Bootstrapping.Tests.InstrumentationTests";
        var testNames = new[] {
            "Initialize_WithDisabledFlag_DoesNotCreateTracerProvider",
            "Initialize_WithDefaultFlag_CreatesTracerProvider",
            "Initialize_WithEnabledFlag_CreatesTracerProvider",
            "Initialize_WithPreviouslyCreatedTracerProvider_WorksCorrectly"
        }.Select(name => $"{testPrefix}.{name}");

        foreach (var testName in testNames)
        {
            DotNetTest(config => config
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoRestore()
                .EnableNoBuild()
                .EnableTrxLogOutput(GetResultsDirectory(project))
                .SetProjectFile(project)
                .SetFilter(testName)
                .SetProcessEnvironmentVariable("BOOSTRAPPING_TESTS", "true"));
        }
    }

    private void RunIntegrationTests()
    {
        Project[] integrationTests = Solution
            .GetProjects("IntegrationTests.*")
            .ToArray();

        DotNetTest(config => config
            .SetConfiguration(BuildConfiguration)
            .SetTargetPlatform(Platform)
            .EnableNoRestore()
            .EnableNoBuild()
            .CombineWith(TestFrameworks.ExceptNetFramework(), (s, fx) => s
                .SetFramework(fx)
            )
            .CombineWith(integrationTests, (s, project) => s
                .EnableTrxLogOutput(GetResultsDirectory(project))
                .SetProjectFile(project)), degreeOfParallelism: 4);
    }
}
