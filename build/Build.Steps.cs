using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    Project NativeProfilerProject => Solution.GetProjectByName(Projects.AutoInstrumentationNative);

    [LazyPathExecutable(name: "cmd")] readonly Lazy<Tool> Cmd;
    [LazyPathExecutable(name: "cmake")] readonly Lazy<Tool> CMake;
    [LazyPathExecutable(name: "make")] readonly Lazy<Tool> Make;

    IEnumerable<MSBuildTargetPlatform> ArchitecturesForPlatform =>
        Equals(Platform, MSBuildTargetPlatform.x64)
            ? [MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86]
            : [MSBuildTargetPlatform.x86];

    private static readonly IEnumerable<TargetFramework> TargetFrameworks =
    [
       TargetFramework.NET8_0,
       TargetFramework.NET462,
    ];

    private static readonly IEnumerable<TargetFramework> TargetFrameworksForNetFxPacking =
    [
        TargetFramework.NET462,
        TargetFramework.NET47,
        TargetFramework.NET471,
        TargetFramework.NET472,
    ];

    private static readonly IEnumerable<TargetFramework> TargetFrameworksForPublish =
    [
        TargetFramework.NET462,
        TargetFramework.NET47,
        TargetFramework.NET471,
        TargetFramework.NET472,
        TargetFramework.NET8_0,
        TargetFramework.NET9_0,
        TargetFramework.NET10_0
    ];

    private static readonly IEnumerable<TargetFramework> TestFrameworks =
    [
        ..TargetFrameworks,
        TargetFramework.NET9_0,
        TargetFramework.NET10_0
    ];

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

    Target GenerateTransientDependencies => _ => _
        .Unlisted()
        .After(Restore)
        .Executes(() =>
        {
            // The target project needs to have its NuGet packages restored prior to running the tool.
            var targetProject = Solution.GetProjectByName(Projects.AutoInstrumentationAssemblies);
            DotNetRestore(s => s.SetProjectFile(targetProject));

            TransientDependenciesGenerator.Run(targetProject);
        });

    Target CompileManagedSrc => _ => _
        .Description("Compiles the managed code in the src directory")
        .After(GenerateTransientDependencies)
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
        .Executes(() =>
        {
            var targetFrameworks = IsWin
                ? TargetFrameworksForPublish
                : TargetFrameworksForPublish.ExceptNetFramework();

            // Publish OpenTelemetry.AutoInstrumentation.Assemblies.NetFramework for all target frameworks
            DotNetPublish(s => s
                .SetProject(Solution.GetProjectByName(Projects.AutoInstrumentationAssemblies))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .SetNoRestore(NoRestore)
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TracerHomeDirectory / framework.OutputFolder / framework)));

            AssertTracerHomeAssemblies(targetFrameworks);

            CleanupTracerHomeAssemblies();

            OptimizeTracerHomeAssemblies(targetFrameworks);
        });

    void AssertTracerHomeAssemblies(IEnumerable<TargetFramework> frameworks)
    {
        // Check that all target framework directories exist
        var missingDirectories = frameworks
            .Select(it => TracerHomeDirectory / it.OutputFolder / it)
            .Where(it => !it.DirectoryExists())
            .ToList();

        if (missingDirectories.Count > 0)
        {
            throw new InvalidOperationException($"Missing framework directories: [{string.Join(", ", missingDirectories)}]");
        }

        // Check that there are no nested directories in target framework folders
        var problematicDirectories = frameworks
            .Select(it => TracerHomeDirectory / it.OutputFolder / it)
            .Where(it => it.GlobDirectories("*").Count != 0)
            .ToList();

        if (problematicDirectories.Count > 0)
        {
            throw new InvalidOperationException($"Loader expects flat framework directories. Subdirectories found in: [{string.Join(", ", problematicDirectories)}]");
        }
    }

    void CleanupTracerHomeAssemblies()
    {
        // remove unnecessary files that may come with target frameworks dependencies
        TracerHomeDirectory.GlobFiles("**/*.pdb", "**/*.xml", "**/*.json", "**/*.dylib", "**/*.so").ForEach(file => file.DeleteFile());
    }

    void OptimizeTracerHomeAssemblies(IEnumerable<TargetFramework> frameworks)
    {
        static bool FilesAreEqual(string filePath1, string filePath2)
        {
            using var hashAlg = SHA256.Create();
            using var stream1 = File.OpenRead(filePath1);
            using var stream2 = File.OpenRead(filePath2);

            var hash1 = hashAlg.ComputeHash(stream1);
            var hash2 = hashAlg.ComputeHash(stream2);

            return hash1.SequenceEqual(hash2);
        }

        // process framework groups separately: .NET Frameworks (/netfx folder) vs .NET (Core) (/net folder)
        // move all duplicated libraries from framework-specific folders to base folder
        foreach (var group in frameworks.GroupBy(it => it.OutputFolder))
        {
            var baseDirectory = TracerHomeDirectory / group.Key;
            var frameworkList = group.ToList();

            baseDirectory.GlobFiles("**/*.link", "**/_._").DeleteFiles();

            var latestFramework = frameworkList.Last();
            var olderFrameworks = frameworkList.TakeUntil(older => older == latestFramework).ToList();

            // Move common files to base directory
            (baseDirectory / latestFramework).GlobFiles("*.*")
                .Where(file => olderFrameworks.All(older =>
                    File.Exists(baseDirectory / older / file.Name) &&
                    FilesAreEqual(file, baseDirectory / older / file.Name)))
                .ForEach(file =>
                {
                    Log.Debug("Move Common File To Base Directory: \"{0}\"", baseDirectory / file.Name);
                    file.MoveToDirectory(baseDirectory, ExistsPolicy.FileOverwrite);
                    olderFrameworks.ForEach(older => (baseDirectory / older / file.Name).DeleteFile());
                });

            // Create link files for duplicates
            frameworkList.Skip(1).Reverse()
                .ForEach(current => (baseDirectory / current).GlobFiles("*.dll")
                    .ForEach(file =>
                    {
                        var linkTarget = frameworkList.TakeUntil(older => older == current)
                            .FirstOrDefault(older => File.Exists(baseDirectory / older / file.Name) &&
                                                    FilesAreEqual(file, baseDirectory / older / file.Name));

                        if (linkTarget != null)
                        {
                            Log.Debug("Generate Link File \"{0}\" To: {1}", file + ".link", linkTarget);
                            file.DeleteFile();
                            (baseDirectory / current / (file.Name + ".link")).WriteAllText(linkTarget, Encoding.ASCII, false);
                        }
                    }));

            // Create placeholder files for empty directories
            frameworkList.Where(fw => (baseDirectory / fw).GlobFiles("*.*").Count == 0)
                .ForEach(fw => (baseDirectory / fw / "_._").WriteAllText(string.Empty, Encoding.ASCII, false));
        }
    }

    Target GenerateAssemblyRedirectionSource => _ => _
        .Unlisted()
        .After(PublishManagedProfiler)
        .Executes(() =>
        {
            if (IsWin)
            {
                // Generate .NET Framework redirects
                // .NET Framework version normalization:
                // net462 -> 462, net47 -> 470, net471 -> 471, net472 -> 472
                // Frameworks with only 2 digits (e.g., 47) are padded with 0 (470)
                AssemblyRedirectionSourceGenerator.Generate(
                    TracerHomeDirectory / TargetFramework.OutputFolderNetFramework,
                    SourceDirectory / Projects.AutoInstrumentationNative / $"assembly_redirection_{TargetFramework.OutputFolderNetFramework}.h",
                    new Regex(@"^net(?<major>\d)(?<minor>\d)(?<patch>\d)?$"),
                    groups => groups["patch"].Success
                        ? groups["major"].Value + groups["minor"].Value + groups["patch"].Value
                        : groups["major"].Value + groups["minor"].Value + "0");
            }

            // Generate .NET (Core) redirects
            // .NET Core version normalization:
            // net8.0 -> 80, net9.0 -> 90, net10.0 -> 100
            AssemblyRedirectionSourceGenerator.Generate(
                TracerHomeDirectory / TargetFramework.OutputFolderNet,
                SourceDirectory / Projects.AutoInstrumentationNative / $"assembly_redirection_{TargetFramework.OutputFolderNet}.h",
                new Regex(@"^net(?<major>\d{1,2})\.(?<minor>\d)$"),
                groups => groups["major"].Value + groups["minor"].Value);
        });

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
