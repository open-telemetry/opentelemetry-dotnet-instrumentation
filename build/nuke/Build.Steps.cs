using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Extensions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Serilog;
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
    AbsolutePath ProfilerTestLogs => BuildDataDirectory / "profiler-logs";
    AbsolutePath AdditionalDepsDirectory => TracerHomeDirectory / "AdditionalDeps";

    Project NativeProfilerProject => Solution.GetProject(Projects.AutoInstrumentationNative);

    [LazyPathExecutable(name: "cmd")] readonly Lazy<Tool> Cmd;
    [LazyPathExecutable(name: "cmake")] readonly Lazy<Tool> CMake;
    [LazyPathExecutable(name: "make")] readonly Lazy<Tool> Make;

    IEnumerable<MSBuildTargetPlatform> ArchitecturesForPlatform =>
        Equals(Platform, MSBuildTargetPlatform.x64)
            ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
            : new[] { MSBuildTargetPlatform.x86 };

    private static readonly IEnumerable<TargetFramework> TargetFrameworks = new[]
    {
        TargetFramework.NET462,
        TargetFramework.NETCOREAPP3_1
    };

    private static readonly IEnumerable<TargetFramework> TestFrameworks = TargetFrameworks
        .Concat(new[] {
            TargetFramework.NET5_0,
            TargetFramework.NET6_0
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
                    .SetProjectFile(MsBuildProject)
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
        .Triggers(CompileManagedTestsWindows)
        .Executes(() =>
        {
            // Always AnyCPU
            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Tests.AutoInstrumentationLoaderTests))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(true));

            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Tests.AutoInstrumentationBootstrappingTests))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(true));

            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Tests.AutoInstrumentationTests))
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

    Target CompileExamples => _ => _
        .Unlisted()
        .Description("Compiles all the example projects")
        .Executes(() =>
        {
            foreach(var exampleProject in Solution.GetProjects("Examples.*"))
            {
                DotNetBuild(s => s
                    .SetProjectFile(exampleProject)
                    .SetConfiguration(BuildConfiguration));
            }
        });

    Target CompileNativeTests => _ => _
        .Unlisted()
        .Description("Compiles the native loader unit tests")
        .DependsOn(CompileNativeTestsWindows)
        .DependsOn(CompileNativeTestsLinux);

    Target PublishManagedProfiler => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .DependsOn(CopyAdditionalDeps)
        .Executes(() =>
        {
            var targetFrameworks = IsWin
                ? TargetFrameworks
                : TargetFrameworks.Where(framework => !framework.ToString().StartsWith("net4"));

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentation))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TracerHomeDirectory / framework)));

            // StartupHook is supported starting .Net Core 3.1.
            // We need to emit AutoInstrumentationStartupHook and AutoInstrumentationLoader assemblies only for .NET Core 3.1 target framework.
            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentationStartupHook))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .SetFramework(TargetFramework.NETCOREAPP3_1)
                .SetOutput(TracerHomeDirectory / TargetFramework.NETCOREAPP3_1));

            // AutoInstrumentationLoader publish is needed only for .Net Core 3.1 to support load from AutoInstrumentationStartupHook.
            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentationLoader))
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

            Log.Information($"Copying '{source}' to '{dest}'");
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
        .Triggers(RunManagedUnitTests)
        .Triggers(RunManagedIntegrationTests)
        .Triggers(RunManagedTestsWindows)
        .Executes(() => { });

    Target PublishMocks => _ => _
        .Unlisted()
        .After(CompileMocks)
        .After(CompileManagedTests)
        .Executes(() =>
        {
            // publish AutoInstrumentation mock
            var targetFrameworks = IsWin
                ? TargetFrameworks
                : TargetFrameworks.ExceptNetFramework();

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.Mocks.AutoInstrumentationMock))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TestsDirectory / Projects.Tests.AutoInstrumentationLoaderTests / "bin" / BuildConfiguration / "Profiler" / framework)));
        });

    Target CompileMocks => _ => _
        .Unlisted()
        .Executes(() =>
        {
            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProject(Projects.Mocks.AutoInstrumentationMock))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(true)
            );
        });

    Target RunManagedUnitTests => _ => _
        .Unlisted()
        .Executes(() =>
        {
            RunBootstrappingTests();

            var unitTestProjects = new[]
            {
                Solution.GetProject(Projects.Tests.AutoInstrumentationLoaderTests),
                Solution.GetProject(Projects.Tests.AutoInstrumentationTests)
            };

            DotNetTest(config => config
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoRestore()
                .EnableNoBuild()
                .CombineWith(unitTestProjects, (s, project) => s
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetProjectFile(project)), degreeOfParallelism: 4);
        });

    Target RunManagedIntegrationTests => _ => _
        .Unlisted()
        .Executes(() =>
        {
            Project[] integrationTests = Solution.GetCrossPlatformIntegrationTests();

            string filter = IsWin ? null : "WindowsOnly!=true";

            DotNetTest(config => config
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatform(Platform)
                .EnableNoRestore()
                .EnableNoBuild()
                .SetFilter(filter)
                .CombineWith(TestFrameworks.ExceptNetFramework(), (s, fx) => s
                    .SetFramework(fx)
                )
                .CombineWith(integrationTests, (s, project) => s
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetProjectFile(project)), degreeOfParallelism: 4);
        });

    Target CopyAdditionalDeps => _ => _
        .Unlisted()
        .Description("Creates AutoInstrumentation.AdditionalDeps and shared store in tracer-home")
        .After(CompileManagedSrc)
        .Executes(() =>
        {
            AdditionalDepsDirectory.GlobFiles("**/*deps.json").ForEach(DeleteFile);

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentationAdditionalDeps))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .SetProperty("TracerHomePath", TracerHomeDirectory)
                .EnableNoBuild()
                .EnableNoRestore()
                .CombineWith(TestFrameworks.ExceptNetFramework(), (p, framework) => p
                    .SetFramework(framework)
                    // Additional-deps probes the directory using SemVer format.
                    // Example: For netcoreapp3.1 framework, additional-deps uses 3.1.0 or 3.1.1 and so on.
                    // Major and Minor version are extracted from framework and default value of 0 is appended for patch.
                    .SetOutput(AdditionalDepsDirectory / "shared" / "Microsoft.NETCore.App" / framework.ToString().Substring(framework.ToString().Length - 3) + ".0")));

            AdditionalDepsDirectory.GlobFiles("**/*.dll", "**/*.pdb", "**/*.xml").ForEach(DeleteFile);
            AdditionalDepsDirectory.GlobFiles("**/*deps.json")
                                   .ForEach(file => {
                                       string depsJsonContent = File.ReadAllText(file);
                                       // Remove OpenTelemetry.Instrumentation.AutoInstrumentationAdditionalDeps entry from target section.
                                       depsJsonContent = Regex.Replace(depsJsonContent, "\"OpenTelemetry(.+)AutoInstrumentation.AdditionalDeps.dll(.+?)}," + Environment.NewLine + "(.+?)\"", "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                       // Remove OpenTelemetry.Instrumentation.AutoInstrumentationAdditionalDeps entry from library section and write to file.
                                       depsJsonContent = Regex.Replace(depsJsonContent, "\"OpenTelemetry(.+?)}," + Environment.NewLine + "(.+?)\"", "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                                       File.WriteAllText(file, depsJsonContent);
                                   });
        });

    private AbsolutePath GetResultsDirectory(Project proj) => BuildDataDirectory / "results" / proj.Name;

    /// <summary>
    /// Bootstrapping tests require every single test to be run in a separate process
    /// so the tracer can be created from scratch for each of them.
    /// </summary>
    private void RunBootstrappingTests()
    {
        var project = Solution.GetProject(Projects.Tests.AutoInstrumentationBootstrappingTests);

        const string testPrefix = "OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests.InstrumentationTests";
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
}
