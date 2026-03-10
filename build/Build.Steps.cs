using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Extensions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Solution("OpenTelemetry.AutoInstrumentation.sln")] readonly Solution Solution;

    AbsolutePath OutputDirectory => RootDirectory / "bin";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "test";

    AbsolutePath TracerHomeDirectory => TracerHome ?? (OutputDirectory / "tracer-home");
    AbsolutePath TestArtifactsDirectory => RootDirectory / "test-artifacts";
    AbsolutePath ProfilerTestLogs => TestArtifactsDirectory / "profiler-logs";
    AbsolutePath AdditionalDepsDirectory => TracerHomeDirectory / "AdditionalDeps";
    AbsolutePath StoreDirectory => TracerHomeDirectory / "store";

    Project NativeProfilerProject => Solution.GetProjectByName(Projects.AutoInstrumentationNative);

    [LazyPathExecutable(name: "cmd")] readonly Lazy<Tool> Cmd;
    [LazyPathExecutable(name: "cmake")] readonly Lazy<Tool> CMake;
    [LazyPathExecutable(name: "make")] readonly Lazy<Tool> Make;

    IEnumerable<MSBuildTargetPlatform> ArchitecturesForPlatform =>
        Equals(Platform, MSBuildTargetPlatform.x64)
            ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
            : new[] { MSBuildTargetPlatform.x86 };

    private static readonly IEnumerable<TargetFramework> TargetFrameworks = new[]
    {
       TargetFramework.NET8_0,
       TargetFramework.NET462,
    };

    private static readonly IEnumerable<TargetFramework> TargetFrameworksForNetFxPacking = new[]
    {
        TargetFramework.NET462,
        TargetFramework.NET47,
        TargetFramework.NET471,
        TargetFramework.NET472,
    };

    private static readonly IEnumerable<TargetFramework> TestFrameworks = TargetFrameworks
        .Concat(TargetFramework.NET9_0, TargetFramework.NET10_0);

    Target CreateRequiredDirectories => _ => _
        .Unlisted()
        .Executes(() =>
        {
            TracerHomeDirectory.CreateDirectory();
            NuGetArtifactsDirectory.CreateDirectory();
            TestArtifactsDirectory.CreateDirectory();
            ProfilerTestLogs.CreateDirectory();
        });

    Target Restore => _ => _
        .After(Clean)
        .Unlisted()
        .Executes(() => ControlFlow.ExecuteWithRetry(() =>
        {
            var projectsToRestore = Solution.GetCrossPlatformManagedProjects();

            if (IsWin)
            {
                projectsToRestore = projectsToRestore.Concat(Solution.GetNetFrameworkOnlyTestApplications());
            }

            foreach (var project in projectsToRestore)
            {
                DotNetRestoreSettings Restore(DotNetRestoreSettings s) =>
                    s.SetProjectFile(project)
                        .SetVerbosity(DotNetVerbosity.normal)
                        .SetProperty("configuration", BuildConfiguration.ToString())
                        .SetPlatform(Platform)
                        .When(_ => !string.IsNullOrEmpty(NuGetPackagesDirectory), o => o.SetPackageDirectory(NuGetPackagesDirectory));

                if (LibraryVersion.TryGetVersions(project.Name, Platform, out var libraryVersions))
                {
                    DotNetRestore(s =>
                         Restore(s)
                            .CombineWithBuildInfos(libraryVersions));
                }
                else
                {
                    DotNetRestore(Restore);
                }
            }

            if (IsWin)
            {
                // Projects using `packages.config` can't be restored via "dotnet restore", use a NuGet Task to restore these projects.
                var legacyRestoreProjects = Solution.GetNativeProjects()
                    .Concat(Solution.GetProjectByName(Projects.Tests.Applications.AspNet))
                    .Concat(Solution.GetProjectByName(Projects.Tests.Applications.WcfIis))
                    .Concat(Solution.GetProjectByName(Projects.Tests.Applications.OwinIis));

                RestoreLegacyNuGetPackagesConfig(legacyRestoreProjects);
            }
        }));

    Target CompileManagedSrc => _ => _
        .Description("Compiles the managed code in the src directory")
        .After(GenerateNetFxTransientDependencies)
        .After(CreateRequiredDirectories)
        .After(Restore)
        .Executes(() =>
        {
            foreach (var project in Solution.GetManagedSrcProjects())
            {
                // Always AnyCPU
                DotNetBuild(x => x
                    .SetProjectFile(project)
                    .SetConfiguration(BuildConfiguration)
                    .SetNoRestore(NoRestore));
            }
        });

    Target CompileManagedTests => _ => _
        .Description("Compiles the managed code in the test directory")
        .After(CompileManagedSrc)
        .After(CompileNativeDependenciesForManagedTests)
        .Executes(() =>
        {
            var testApps = Solution.GetCrossPlatformTestApplications();
            if (IsWin)
            {
                if (TestTargetFramework == TargetFramework.NET462 ||
                    TestTargetFramework == TargetFramework.NOT_SPECIFIED)
                {
                    testApps = Solution.GetNetFrameworkOnlyTestApplications().Concat(testApps);
                }
                else
                {
                    // Special case: some WCF .NET tests need a WCF server app that only builds for .NET Framework 4.6.2
                    DotNetBuild(s => s
                        .SetProjectFile(Solution.GetProjectByName(Projects.Tests.Applications.WcfServer))
                        .SetConfiguration(BuildConfiguration)
                        .SetPlatform(Platform)
                        .SetNoRestore(NoRestore)
                        .SetFramework(TargetFramework.NET462));
                }
            }

            foreach (var app in testApps)
            {

                // Special case: a test application using old packages.config needs special treatment.
                var legacyPackagesConfig = app.Directory.ContainsFile("packages.config");
                if (legacyPackagesConfig)
                {
                    PerformLegacyRestoreIfNeeded(app);

                    DotNetBuild(s => s
                        .SetProjectFile(app)
                        .SetNoRestore(true)  // project w/ packages.config can't do the restore via dotnet CLI
                        .SetPlatform(Platform)
                        .SetConfiguration(BuildConfiguration)
                        .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED,
                            x => x.SetFramework(TestTargetFramework)));

                    continue;
                }

                string actualTestTfm = TestTargetFramework;
                if (TestTargetFramework != TargetFramework.NOT_SPECIFIED &&
                    !app.GetTargetFrameworks().Contains(actualTestTfm))
                {
                    // Before skipping this app check if not a special case for .NET Framework
                    actualTestTfm = null;
                    if (TestTargetFramework == TargetFramework.NET462)
                    {
                        actualTestTfm = app.GetTargetFrameworks().FirstOrDefault(tfm => tfm.StartsWith("net4"));
                    }

                    if (actualTestTfm is null)
                    {
                        // App doesn't support the select TFM, skip it.
                        Log.Information("Skipping {0}: no suitable TFM for {1}", app.Name, TestTargetFramework);
                        continue;
                    }
                }

                DotNetBuildSettings BuildTestApplication(DotNetBuildSettings x, string targetFramework) =>
                    x.SetProjectFile(app)
                        .SetConfiguration(BuildConfiguration)
                        .SetPlatform(Platform)
                        .SetNoRestore(NoRestore)
                        .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED,
                        s => s.SetFramework(targetFramework));

                if (LibraryVersion.Versions.TryGetValue(app.Name, out var libraryVersions))
                {
                    foreach (var packageBuildInfo in libraryVersions)
                    {
                        var targetFramework = packageBuildInfo.SupportedFrameworks.Length == 0 || packageBuildInfo.SupportedFrameworks.Contains(actualTestTfm) ? actualTestTfm : TestTargetFramework;
                        DotNetBuild(x =>
                            BuildTestApplication(x, targetFramework)
                                .CombineWithBuildInfos([packageBuildInfo], TestTargetFramework));
                    }
                }
                else
                {
                    DotNetBuild(x => BuildTestApplication(x, actualTestTfm));
                }
            }

            foreach (var project in Solution.GetManagedTestProjects())
            {
                if (TestTargetFramework != TargetFramework.NOT_SPECIFIED &&
                    !project.GetTargetFrameworks().Contains(TestTargetFramework))
                {
                    // Skip this test project if it doesn't support the selected test TFM.
                    continue;
                }

                // Always AnyCPU
                DotNetBuild(x => x
                    .SetProjectFile(project)
                    .SetConfiguration(BuildConfiguration)
                    .SetNoRestore(NoRestore)
                    .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED,
                        s => s.SetFramework(TestTargetFramework)));
            }
        });

    Target CompileNativeDependenciesForManagedTests => _ => _
        .Description("Compiles the native dependencies for testing applications")
        .DependsOn(CompileNativeDependenciesForManagedTestsWindows)
        .DependsOn(CompileNativeDependenciesForManagedTestsLinux)
        .DependsOn(CompileNativeDependenciesForManagedTestsMacOs);

    Target CompileNativeSrc => _ => _
        .Description("Compiles the native loader")
        .DependsOn(CompileNativeSrcWindows)
        .DependsOn(CompileNativeSrcLinux)
        .DependsOn(CompileNativeSrcMacOs);

    Target CompileNativeTests => _ => _
        .Description("Compiles the native loader unit tests")
        .DependsOn(CompileNativeTestsWindows)
        .DependsOn(CompileNativeTestsLinux);

    Target CompileExamples => _ => _
        .Description("Compiles all the example projects")
        .Executes(() =>
        {
            foreach (var exampleProject in Solution.GetAllProjects("Examples.*"))
            {
                DotNetBuild(s => s
                    .SetProjectFile(exampleProject)
                    .SetConfiguration(BuildConfiguration));
            }
        });

    Target PublishManagedProfiler => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .DependsOn(CopyAdditionalDeps)
        .Executes(() =>
        {
            var targetFrameworks = IsWin
                ? TargetFrameworks
                : TargetFrameworks.ExceptNetFramework();

            // Publish Projects.AutoInstrumentation for .NET targets
            DotNetPublish(s => s
                .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentation))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .CombineWith(targetFrameworks.ExceptNetFramework(), (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TracerHomeDirectory / MapToFolderOutput(framework))));

            if (IsWin)
            {
                // Publish OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework for .NET Framework targets
                DotNetPublish(s => s
                    .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentationNetFxAssemblies))
                    .SetConfiguration(BuildConfiguration)
                    .SetTargetPlatformAnyCPU()
                    .EnableNoBuild()
                    .SetNoRestore(NoRestore)
                    .CombineWith(TargetFrameworksForNetFxPacking, (p, framework) => p
                        .SetFramework(framework)
                        .SetOutput(TracerHomeDirectory / MapToFolderOutputNetFx(framework))));
            }

            // StartupHook is supported starting .Net Core 3.1.
            // We need to emit AutoInstrumentationStartupHook for .Net Core 3.1 target framework
            // to avoid application crash with .Net Core 3.1 and .NET 5.0 apps.
            DotNetPublish(s => s
                .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentationStartupHook))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .SetFramework(TargetFramework.NETCore3_1)
                .SetOutput(TracerHomeDirectory / MapToFolderOutput(TargetFramework.NETCore3_1)));

            // AutoInstrumentationLoader publish is needed only for .NET 8.0 to support load from AutoInstrumentationStartupHook.
            DotNetPublish(s => s
                .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentationLoader))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .SetFramework(TargetFramework.NET8_0)
                .SetOutput(TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET8_0)));

            DotNetPublish(s => s
                .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentationAspNetCoreBootstrapper))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .SetFramework(TargetFramework.NET8_0)
                .SetOutput(TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET8_0)));

            RemoveFilesInNetFolderAvailableInAdditionalStore();

            RemoveNonLibraryFilesFromOutput();

            RemoveDuplicateNetFxLibraries();
        });

    void RemoveNonLibraryFilesFromOutput()
    {
        TracerHomeDirectory.GlobFiles("**/*.xml").ForEach(file => file.DeleteFile());
        (TracerHomeDirectory / "net").GlobFiles("*.json").ForEach(file => file.DeleteFile());
        if (IsWin)
        {
            (TracerHomeDirectory / "netfx").GlobFiles("*.json").ForEach(file => file.DeleteFile());
        }
    }

    void RemoveFilesInNetFolderAvailableInAdditionalStore()
    {
        Log.Debug("Removing files available in additional store from net folder");
        var netFolder = TracerHomeDirectory / "net";
        var additionalStoreFolder = TracerHomeDirectory / "store";

        var netLibraries = netFolder.GlobFiles("**/*.dll");
        var netLibrariesByName = netLibraries.ToDictionary(x => x.Name);
        var additionalStoreLibraries = additionalStoreFolder.GlobFiles("**/*.dll");

        foreach (var additionalStoreLibrary in additionalStoreLibraries)
        {
            if (netLibrariesByName.TryGetValue(additionalStoreLibrary.Name, out var netLibrary))
            {
                var netLibraryFileVersionInfo = FileVersionInfo.GetVersionInfo(netLibrary);
                var additionalStoreLibraryFileVersionInfo = FileVersionInfo.GetVersionInfo(additionalStoreLibrary);

                if (netLibraryFileVersionInfo.FileVersion == additionalStoreLibraryFileVersionInfo.FileVersion)
                {
                    Log.Debug("Delete file available in additional store from net folder " + additionalStoreLibrary.Name + " version: " + netLibraryFileVersionInfo.FileVersion);
                    netLibrary.DeleteFile();
                    netLibrariesByName.Remove(additionalStoreLibrary.Name);
                }
                else
                {
                    Log.Warning("Cannot remove file available in additional store from net folder " + additionalStoreLibrary.Name + " net folder version: " + netLibraryFileVersionInfo.FileVersion + " additional store version: " + additionalStoreLibraryFileVersionInfo.FileVersion);
                }
            }
        }
    }

    void RemoveDuplicateNetFxLibraries()
    {
        bool FilesAreEqual(string filePath1, string filePath2)
        {
            using var hashAlg = SHA256.Create();
            using var stream1 = File.OpenRead(filePath1);
            using var stream2 = File.OpenRead(filePath2);

            var hash1 = hashAlg.ComputeHash(stream1);
            var hash2 = hashAlg.ComputeHash(stream2);

            return hash1.SequenceEqual(hash2);
        }

        if (IsWin)
        {
            (TracerHomeDirectory / "netfx").GlobFiles("**/*.link").DeleteFiles();
            (TracerHomeDirectory / "netfx").GlobFiles("**/_._").DeleteFiles();
            var latestFramework = TargetFramework.NetFramework.Last();
            (TracerHomeDirectory / "netfx" / latestFramework).GlobFiles("*.*")
                .Where(file => TargetFramework.NetFramework.TakeUntil(older => older == latestFramework)
                    .All(olderFramework =>
                    {
                        var duplicateCandidate = TracerHomeDirectory / "netfx" / olderFramework / file.Name;
                        return File.Exists(duplicateCandidate) && FilesAreEqual(file, duplicateCandidate);
                    })).ForEach(file =>
                    {
                        file.MoveToDirectory(TracerHomeDirectory / "netfx", ExistsPolicy.FileOverwrite);
                        TargetFramework.NetFramework.TakeUntil(older => older == latestFramework)
                            .ForEach(olderFramework =>
                                (TracerHomeDirectory / "netfx" / olderFramework / file.Name).DeleteFile());
                    }
                );

            foreach (var currentFramework in TargetFramework.NetFramework.Skip(1).Reverse())
            {
                (TracerHomeDirectory / "netfx" / currentFramework).GlobFiles("*.dll").ForEach(file =>
                    {
                        foreach (var olderFramework in TargetFramework.NetFramework.TakeUntil(older =>
                                     older == currentFramework))
                        {
                            var duplicateCandidate = TracerHomeDirectory / "netfx" / olderFramework / file.Name;
                            if (File.Exists(duplicateCandidate) && FilesAreEqual(file, duplicateCandidate))
                            {
                                file.DeleteFile();
                                (TracerHomeDirectory / "netfx" / currentFramework / (file.Name + ".link")).WriteAllText(
                                    olderFramework, Encoding.ASCII, false);
                                break;
                            }
                        }
                    }
                );
            }

            // Create placeholder file for empty directories
            foreach (var currentFramework in TargetFramework.NetFramework)
            {
                if ((TracerHomeDirectory / "netfx" / currentFramework).GlobFiles("*.*").Count == 0)
                {
                    (TracerHomeDirectory / "netfx" / currentFramework / "_._")
                        .WriteAllText(string.Empty, Encoding.ASCII, false);
                }
            }
        }
    }

    Target PublishNativeProfiler => _ => _
        .Unlisted()
        .DependsOn(PublishNativeProfilerWindows)
        .DependsOn(PublishNativeProfilerLinux)
        .DependsOn(PublishNativeProfilerMacOs);

    Target VerifySdkVersions => _ => _
        .Executes(() =>
        {
            var verifier = Solution.GetProjectByName(Projects.Tools.SdkVersionAnalyzerTool);

            DotNetRun(s => s
                .SetProjectFile(verifier)
                .SetApplicationArguments("--verify", RootDirectory));
        });

    Target GenerateLibraryVersionFiles => _ => _
        .After(PublishManagedProfiler)
        .Executes(() =>
        {
            var generatorTool = Solution.GetProjectByName(Projects.Tools.LibraryVersionsGenerator);

            DotNetRun(s => s
                .SetProjectFile(generatorTool));
        });

    Target CopyInstrumentScripts => _ => _
        .Unlisted()
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var source = RootDirectory / "instrument.sh";
            var dest = TracerHomeDirectory;
            source.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
        });

    Target CopyLegalFiles => _ => _
        .Unlisted()
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var source = RootDirectory / "LICENSE";
            var dest = TracerHomeDirectory;
            source.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
        });

    Target CreateVersionFile => _ => _
        .Unlisted()
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var version = VersionHelper.GetVersion();
            var refName = "local-dev";
            var gitSha = VersionHelper.GetCommitId();

            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") is "true")
            {
                refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
            }

            var dest = TracerHomeDirectory / "VERSION";
            dest.WriteAllLines([version, $"{refName}@{gitSha}"]);
        });

    Target RunNativeTests => _ => _
        .Unlisted()
        .DependsOn(RunNativeTestsWindows)
        .DependsOn(RunNativeTestsLinux);

    Target RunManagedTests => _ => _
        .Unlisted()
        .Produces(TestArtifactsDirectory / "profiler-logs" / "*")
        .After(BuildTracer)
        .After(CompileManagedTests)
        .After(PublishMocks)
        .Triggers(RunManagedUnitTests)
        .Triggers(RunManagedIntegrationTests)
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
            if (TestTargetFramework != TargetFramework.NOT_SPECIFIED)
            {
                if (!targetFrameworks.Contains(TestTargetFramework))
                {
                    // This test doesn't run for the selected test TFM, nothing to do.
                    return;
                }

                targetFrameworks = new[] { TestTargetFramework };
            }

            DotNetPublish(s => s
                .SetProject(Solution.GetTestMock())
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TestsDirectory / Projects.Tests.AutoInstrumentationLoaderTests / "bin" / BuildConfiguration / "Profiler" / framework)));
        });

    Target CompileMocks => _ => _
        .Unlisted()
        .Executes(() =>
        {
            DotNetBuild(x => x
                .SetProjectFile(Solution.GetProjectByName(Projects.Mocks.AutoInstrumentationMock))
                .SetConfiguration(BuildConfiguration)
                .SetNoRestore(NoRestore)
                .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED,
                    s => s.SetFramework(TestTargetFramework))
            );
        });

    Target RunManagedUnitTests => _ => _
        .Unlisted()
        .Executes(() =>
        {
            RunBootstrappingTests();

            var unitTestProjects = new[]
            {
                Solution.GetProjectByName(Projects.Tests.AutoInstrumentationBuildTasksTests),
                Solution.GetProjectByName(Projects.Tests.AutoInstrumentationLoaderTests),
                Solution.GetProjectByName(Projects.Tests.AutoInstrumentationStartupHookTests),
                Solution.GetProjectByName(Projects.Tests.AutoInstrumentationTests)
            };

            if (!string.IsNullOrWhiteSpace(TestProject))
            {
                unitTestProjects = unitTestProjects
                    .Where(p => p.Name.Contains(TestProject, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (unitTestProjects.Length == 0)
                {
                    return;
                }
            }

            if (TestTargetFramework != TargetFramework.NOT_SPECIFIED)
            {
                unitTestProjects = unitTestProjects
                    .Where(p =>
                        p.GetTargetFrameworks().Contains(TestTargetFramework) &&
                        (p.Name != Projects.Tests.AutoInstrumentationLoaderTests || TargetFrameworks.Contains(TestTargetFramework)))
                    .ToArray();
            }

            for (int i = 0; i < TestCount; i++)
            {
                DotNetTest(config => config
                    .SetConfiguration(BuildConfiguration)
                    .SetTargetPlatformAnyCPU()
                    .SetFilter(TestNameFilter())
                    .SetNoRestore(NoRestore)
                    .SetProcessEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", ProfilerTestLogs)
                    .EnableNoBuild()
                    .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED,
                        x => x.SetFramework(TestTargetFramework))
                    .CombineWith(unitTestProjects, (s, project) => s
                        .EnableTrxLogOutput(GetResultsDirectory(project))
                        .SetProjectFile(project)), degreeOfParallelism: 4);
            }
        });

    Target RunManagedIntegrationTests => _ => _
        .Unlisted()
        .After(InstallNetFxAssembliesGAC)
        .After(RunManagedUnitTests)
        .Executes(() =>
        {
            var project = Solution.GetManagedIntegrationTestProject();
            if (!string.IsNullOrWhiteSpace(TestProject) && !project.Name.Contains(TestProject, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            for (int i = 0; i < TestCount; i++)
            {
                DotNetTest(config => config
                    .SetConfiguration(BuildConfiguration)
                    .SetFilter(AndFilter(TestNameFilter(), ContainersFilter()))
                    .SetBlameHangTimeout("5m")
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetProjectFile(project)
                    .SetNoRestore(NoRestore)
                    .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED,
                        s => s.SetFramework(TestTargetFramework))
                );
            }
        });

    Target CopyAdditionalDeps => _ => _
        .Unlisted()
        .Description("Creates AutoInstrumentation.AdditionalDeps and shared store in tracer-home")
        .After(CompileManagedSrc)
        .Executes(() =>
        {
            if (AdditionalDepsDirectory.DirectoryExists())
            {
                Directory.Delete(AdditionalDepsDirectory, true);
            }

            if (StoreDirectory.DirectoryExists())
            {
                Directory.Delete(StoreDirectory, true);
            }

            DotNetPublish(s => s
                .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentationAdditionalDeps))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .SetProperty("NukePlatform", Platform)
                .SetProperty("TracerHomePath", TracerHomeDirectory)
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .CombineWith(TestFrameworks.ExceptNetFramework(), (p, framework) => p
                .SetFramework(framework)
                // Additional-deps probes the directory using SemVer format.
                // Example: For net8.0, additional-deps uses 8.0.0.
                // Major and Minor version are extracted from framework and default value of 0 is appended for patch.
                .SetOutput(AdditionalDepsDirectory / "shared" / "Microsoft.NETCore.App" / framework.ToString().Substring("net".Length) + ".0")));

            AdditionalDepsDirectory.GlobFiles("**/*deps.json")
                .ForEach(file =>
                {
                    var rawJson = File.ReadAllText(file);
                    var depsJson = JsonNode.Parse(rawJson).AsObject();

                    var folderRuntimeName = depsJson.GetFolderRuntimeName();
                    var architectureStores = new List<AbsolutePath>()
                        .AddIf(StoreDirectory / "x64" / folderRuntimeName, RuntimeInformation.OSArchitecture == Architecture.X64)
                        .AddIf(StoreDirectory / "x86" / folderRuntimeName, IsWin) // Only Windows supports x86 runtime
                        .AddIf(StoreDirectory / "arm64" / folderRuntimeName, IsArm64)
                        .AsReadOnly();

                    depsJson.CopyNativeDependenciesToStore(file, architectureStores);
                    depsJson.RemoveDuplicatedLibraries(architectureStores);
                    depsJson.RemoveOpenTelemetryLibraries();

                    // To allow roll forward for applications, like Roslyn, that target one tfm
                    // but have a later runtime move the libraries under the original tfm folder
                    // to the latest one.
                    if (folderRuntimeName == TargetFramework.NET8_0 || folderRuntimeName == TargetFramework.NET9_0 || folderRuntimeName == TargetFramework.NET10_0)
                    {
                        depsJson.RollFrameworkForward(TargetFramework.NET8_0, TargetFramework.NET10_0, architectureStores);
                        depsJson.RollFrameworkForward(TargetFramework.NET9_0, TargetFramework.NET10_0, architectureStores);
                    }

                    // Write the updated deps.json file.
                    File.WriteAllText(file, depsJson.ToJsonString(new()
                    {
                        WriteIndented = true
                    }));
                });

            // Cleanup Additional Deps Directory
            AdditionalDepsDirectory.GlobFiles("**/*.dll", "**/*.pdb", "**/*.xml", "**/*.dylib", "**/*.so").ForEach(file => file.DeleteFile());
            AdditionalDepsDirectory.GlobDirectories("**/runtimes").ForEach(directory => directory.DeleteDirectory());
        });

    Target PublishRuleEngineJson => _ => _
        .After(PublishManagedProfiler)
        .Description("Publishes a file with assembly name and version for rule engine validation.")
        .Executes(() =>
        {
            var netPath = TracerHomeDirectory / "net";
            var files = Directory.GetFiles(netPath);
            var fileInfoList = new List<object>(files.Length);

            foreach (string file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName == "System.Diagnostics.DiagnosticSource" ||
                    (fileName.StartsWith("OpenTelemetry.") && !fileName.StartsWith("OpenTelemetry.Api") && !fileName.StartsWith("OpenTelemetry.AutoInstrumentation")))
                {
                    var fileVersion = FileVersionInfo.GetVersionInfo(file).FileVersion;
                    fileInfoList.Add(new
                    {
                        FileName = fileName,
                        FileVersion = fileVersion
                    });
                }
            }

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string jsonContent = JsonSerializer.Serialize(fileInfoList, options);

            var ruleEngineJsonFilePath = netPath / "ruleEngine.json";
            File.WriteAllText(ruleEngineJsonFilePath, jsonContent);

            var ruleEngineJsonNugetFilePath = RootDirectory / "nuget" / "OpenTelemetry.AutoInstrumentation" / "contentFiles" / "any" / "any" / "RuleEngine.json";
            File.Delete(ruleEngineJsonNugetFilePath);
            File.Copy(ruleEngineJsonFilePath, ruleEngineJsonNugetFilePath);
        });

    Target InstallDocumentationTools => _ => _
        .Description("Installs markdownlint-cli and cspell locally. npm is required")
        .Executes(() =>
        {
            NpmTasks.NpmInstall();
        });

    Target MarkdownLint => _ => _
        .Description("Executes MarkdownLint")
        .After(InstallDocumentationTools)
        .Executes(() =>
        {
            NpmTasks.NpmRun(s => s.SetCommand(@"markdownlint"));
        });

    Target MarkdownLintFix => _ => _
        .Description("Trying to fix MarkdownLint issues")
        .After(InstallDocumentationTools)
        .Executes(() =>
        {
            NpmTasks.NpmRun(s => s.SetCommand(@"markdownlint-fix"));
        });

    Target SpellcheckDocumentation => _ => _
        .Description("Executes MarkdownLint")
        .After(InstallDocumentationTools)
        .Executes(() =>
        {
            NpmTasks.NpmRun(s => s.SetCommand(@"cspell"));
        });

    Target ValidateDocumentation => _ => _
        .Description("Executes validation tools for documentation")
        .DependsOn(MarkdownLint)
        .DependsOn(SpellcheckDocumentation);

    private AbsolutePath GetResultsDirectory(Project proj) => TestArtifactsDirectory / "results" / proj.Name;

    /// <summary>
    /// Bootstrapping tests require every single test to be run in a separate process
    /// so the tracer can be created from scratch for each of them.
    /// </summary>
    private void RunBootstrappingTests()
    {
        var project = Solution.GetProjectByName(Projects.Tests.AutoInstrumentationBootstrappingTests);
        if (!string.IsNullOrWhiteSpace(TestProject) && !project.Name.Contains(TestProject, StringComparison.OrdinalIgnoreCase))
        {
            // Test project was not selected.
            return;
        }

        const string testPrefix = "OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests.InstrumentationTests";
        var testNames = new[] {
            "Initialize_WithDisabledFlag_DoesNotCreateTracerProvider",
            "Initialize_WithDefaultFlag_CreatesTracerProvider",
            "Initialize_WithEnabledFlag_CreatesTracerProvider",
            "Initialize_WithPreviouslyCreatedTracerProvider_WorksCorrectly"
        }.Select(name => $"FullyQualifiedName~{testPrefix}.{name}");

        for (int i = 0; i < TestCount; i++)
        {
            foreach (var testName in testNames)
            {
                DotNetTest(config => config
                    .SetConfiguration(BuildConfiguration)
                    .SetTargetPlatformAnyCPU()
                    .SetNoRestore(NoRestore)
                    .EnableNoBuild()
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetProjectFile(project)
                    .SetFilter(AndFilter(TestNameFilter(), testName))
                    .When(_ => TestTargetFramework != TargetFramework.NOT_SPECIFIED, s => s.SetFramework(TestTargetFramework))
                    .SetProcessEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", ProfilerTestLogs)
                    .SetProcessEnvironmentVariable("BOOSTRAPPING_TESTS", "true"));
            }
        }
    }

    private string MapToFolderOutput(TargetFramework targetFramework)
    {
        return targetFramework.ToString().StartsWith("net4") ? "netfx" : "net";
    }

    private string MapToFolderOutputNetFx(TargetFramework targetFramework)
    {
        return $"netfx/{targetFramework}";
    }

    private void RestoreLegacyNuGetPackagesConfig(IEnumerable<Project> legacyRestoreProjects)
    {
        foreach (var project in legacyRestoreProjects)
        {
            // Restore legacy projects
            NuGetTasks.NuGetRestore(s => s
                .SetTargetPath(project)
                .SetSolutionDirectory(Solution.Directory)
                .SetVerbosity(NuGetVerbosity.Normal)
                .When(_ => !string.IsNullOrEmpty(NuGetPackagesDirectory), o =>
                    o.SetPackagesDirectory(NuGetPackagesDirectory)));
        }
    }
}
