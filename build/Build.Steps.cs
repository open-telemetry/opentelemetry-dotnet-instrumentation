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
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Solution("OpenTelemetry.AutoInstrumentation.sln")] readonly Solution Solution;

    AbsolutePath OutputDirectory => RootDirectory / "bin";
    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "test";

    AbsolutePath TracerHomeDirectory => TracerHome ?? (OutputDirectory / "tracer-home");
    AbsolutePath ArtifactsDirectory => Artifacts ?? (OutputDirectory / "artifacts");
    AbsolutePath BuildDataDirectory => RootDirectory / "build_data";
    AbsolutePath ProfilerTestLogs => BuildDataDirectory / "profiler-logs";
    AbsolutePath AdditionalDepsDirectory => TracerHomeDirectory / "AdditionalDeps";
    AbsolutePath StoreDirectory => TracerHomeDirectory / "store";

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
        TargetFramework.NET6_0
    };

    private static readonly IEnumerable<TargetFramework> TestFrameworks = TargetFrameworks
        .Concat(new[] {
            TargetFramework.NET7_0
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
        .Executes(() => ControlFlow.ExecuteWithRetry(() =>
        {
            var projectsToRestore = Solution.GetCrossPlatformManagedProjects();

            if (IsWin)
            {
                projectsToRestore = projectsToRestore.Concat(Solution.GetWindowsOnlyTestApplications());
            }

            foreach (var project in projectsToRestore)
            {
                if (TestPackageVersions.Versions.TryGetValue(project.Name, out var testedPackageVersions))
                {
                    foreach (var testedPackageVersion in testedPackageVersions)
                    {
                        DotNetRestore(s => s
                            .SetProjectFile(project)
                            .SetVerbosity(DotNetVerbosity.Normal)
                            .SetProperty("configuration", BuildConfiguration.ToString())
                            .SetProperty("TestedPackageVersion", testedPackageVersion)
                            .SetPlatform(Platform)
                            .When(!string.IsNullOrEmpty(NugetPackageDirectory), o =>
                                o.SetPackageDirectory(NugetPackageDirectory)));
                    }
                }
                else
                {
                    DotNetRestore(s =>  s
                            .SetProjectFile(project)
                            .SetVerbosity(DotNetVerbosity.Normal)
                            .SetProperty("configuration", BuildConfiguration.ToString())
                            .SetPlatform(Platform)
                            .When(!string.IsNullOrEmpty(NugetPackageDirectory), o =>
                                o.SetPackageDirectory(NugetPackageDirectory)));
                }
            }

            if (IsWin)
            {
                // Projects using `packages.config` can't be restored via "dotnet restore", use a NuGet Task to restore these projects.
                var legacyRestoreProjects = Solution.GetNativeProjects()
                    .Concat(new[] { Solution.GetProject(Projects.Tests.Applications.AspNet) });

                foreach (var project in legacyRestoreProjects)
                {
                    // Restore legacy projects
                    NuGetTasks.NuGetRestore(s => s
                        .SetTargetPath(project)
                        .SetSolutionDirectory(Solution.Directory)
                        .SetVerbosity(NuGetVerbosity.Normal)
                        .When(!string.IsNullOrEmpty(NugetPackageDirectory), o =>
                            o.SetPackagesDirectory(NugetPackageDirectory)));
                }
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
                    .EnableNoRestore());
            }
        });

    Target CompileManagedTests => _ => _
        .Description("Compiles the managed code in the test directory")
        .After(CompileManagedSrc)
        .Executes(() =>
        {
            var testApps = Solution.GetCrossPlatformTestApplications();
            if (IsWin)
            {
                testApps = testApps.Concat(Solution.GetWindowsOnlyTestApplications());
            }

            foreach (var app in testApps)
            {
                if (TestPackageVersions.Versions.TryGetValue(app.Name, out var testedPackageVersions))
                {
                    foreach (var testedPackageVersion in testedPackageVersions)
                    {
                        DotNetBuild(x => x
                            .SetProjectFile(app)
                            .SetConfiguration(BuildConfiguration)
                            .SetProperty("TestedPackageVersion", testedPackageVersion)
                            .SetPlatform(Platform)
                            .SetNoRestore(true));
                    }
                }
                else
                {
                    DotNetBuild(x => x
                        .SetProjectFile(app)
                        .SetConfiguration(BuildConfiguration)
                        .SetPlatform(Platform)
                        .SetNoRestore(true));
                }
            }

            foreach (var project in Solution.GetManagedTestProjects())
            {
                // Always AnyCPU
                DotNetBuild(x => x
                    .SetProjectFile(project)
                    .SetConfiguration(BuildConfiguration)
                    .SetNoRestore(true));
            }
        });

    Target CompileBenchmarks => _ => _
        .Description("Compiles the Benchmarks project in the test directory")
        .After(CompileManagedSrc)
        .Executes(() =>
        {
            DotNetBuild(x => x
                .SetProjectFile(Solution.GetBenchmarks())
                .SetConfiguration(BuildConfiguration)
                .EnableNoRestore());
        });

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
            foreach (var exampleProject in Solution.GetProjects("Examples.*"))
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
                : TargetFrameworks.Where(framework => !framework.ToString().StartsWith("net4"));

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentation))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .CombineWith(targetFrameworks, (p, framework) => p
                    .SetFramework(framework)
                    .SetOutput(TracerHomeDirectory / MapToFolderOutput(framework))));

            // StartupHook is supported starting .Net Core 3.1.
            // We need to emit AutoInstrumentationStartupHook for .Net Core 3.1 target framework
            // to avoid application crash with .Net Core 3.1 and .NET 5.0 apps.
            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentationStartupHook))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .SetFramework(TargetFramework.NETCore3_1)
                .SetOutput(TracerHomeDirectory / MapToFolderOutput(TargetFramework.NETCore3_1)));

            // AutoInstrumentationLoader publish is needed only for .NET 6.0 to support load from AutoInstrumentationStartupHook.
            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentationLoader))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .SetFramework(TargetFramework.NET6_0)
                .SetOutput(TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET6_0)));

            DotNetPublish(s => s
                .SetProject(Solution.GetProject(Projects.AutoInstrumentationAspNetCoreBootstrapper))
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatformAnyCPU()
                .EnableNoBuild()
                .EnableNoRestore()
                .SetFramework(TargetFramework.NET6_0)
                .SetOutput(TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET6_0)));

            // Remove non-library files
            TracerHomeDirectory.GlobFiles("**/*.xml").ForEach(DeleteFile);
            (TracerHomeDirectory / "net").GlobFiles("*.json").ForEach(DeleteFile);
            if (IsWin)
            {
                (TracerHomeDirectory / "netfx").GlobFiles("*.json").ForEach(DeleteFile);
            }
        });

    Target PublishNativeProfiler => _ => _
        .Unlisted()
        .DependsOn(PublishNativeProfilerWindows)
        .DependsOn(PublishNativeProfilerLinux)
        .DependsOn(PublishNativeProfilerMacOs);

    Target GenerateIntegrationsJson => _ => _
        .After(PublishManagedProfiler)
        .Executes(() =>
        {
            var generatorTool = Solution.GetProject(Projects.Tools.IntegrationsJsonGenerator);

            DotNetRun(s => s
                .SetProjectFile(generatorTool));
        });

    Target CopyIntegrationsJson => _ => _
        .Unlisted()
        .After(GenerateIntegrationsJson)
        .Executes(() =>
        {
            var source = RootDirectory / "integrations.json";
            var dest = TracerHomeDirectory;
            CopyFileToDirectory(source, dest, FileExistsPolicy.Overwrite);
        });

    Target CopyInstrumentScripts => _ => _
        .Unlisted()
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var source = RootDirectory / "instrument.sh";
            var dest = TracerHomeDirectory;
            CopyFileToDirectory(source, dest, FileExistsPolicy.Overwrite);
        });

    Target CopyLegalFiles => _ => _
        .Unlisted()
        .After(Clean)
        .After(CreateRequiredDirectories)
        .Executes(() =>
        {
            var source = RootDirectory / "LICENSE";
            var dest = TracerHomeDirectory;
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
                .SetProject(Solution.GetTestMock())
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

            for (int i = 0; i < TestCount; i++)
            {
                DotNetTest(config => config
                    .SetConfiguration(BuildConfiguration)
                    .SetTargetPlatformAnyCPU()
                    .SetFilter(TestNameFilter())
                    .EnableNoRestore()
                    .EnableNoBuild()
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

            var frameworks = IsWin ? TestFrameworks : TestFrameworks.ExceptNetFramework();

            for (int i = 0; i < TestCount; i++)
            {
                DotNetMSBuild(config => config
                    .SetConfiguration(BuildConfiguration)
                    .SetFilter(AndFilter(TestNameFilter(), ContainersFilter()))
                    .SetBlameHangTimeout("5m")
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetTargetPath(project)
                    .DisableRestore()
                    .RunTests()
                );
            }
        });

    Target CopyAdditionalDeps => _ =>
    {
        return _
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

                AdditionalDepsDirectory.GlobFiles("**/*deps.json")
                    .ForEach(file =>
                    {
                        var depsJsonContent = File.ReadAllText(file);
                        using var jsonDocument = JsonDocument.Parse(depsJsonContent);

                        var folderRuntimeName = GetFolderRuntimeName(jsonDocument);

                        var architectureStores = new List<string>
                        {
                            Path.Combine(StoreDirectory, "x64", folderRuntimeName),
                            Path.Combine(StoreDirectory, "x86", folderRuntimeName),
                        }.AsReadOnly();

                        CopyNativeDependenciesToStore(file, jsonDocument, architectureStores);

                        RemoveDuplicatedLibraries(depsJsonContent, architectureStores);

                        RemoveOpenTelemetryAutoInstrumentationAdditionalDepsFromDepsFile(depsJsonContent, file);

                        // To allow roll forward for applications, like Roslyn, that target one tfm
                        // but have a later runtime make additional copies under the original tfm folder.
                        if (folderRuntimeName == TargetFramework.NET6_0)
                        {
                            AddFrameworkRollForwardCopy(TargetFramework.NET6_0, TargetFramework.NET7_0, architectureStores);
                        }
                    });
                RemoveFilesFromAdditionalDepsDirectory();


                void CopyNativeDependenciesToStore(AbsolutePath file, JsonDocument jsonDocument, IReadOnlyList<string> architectureStores)
                {
                    var depsDirectory = file.Parent;

                    foreach (var targetProperty in jsonDocument.RootElement.GetProperty("targets").EnumerateObject())
                    {
                        var target = targetProperty.Value;

                        foreach (var packages in target.EnumerateObject())
                        {
                            if (!packages.Value.TryGetProperty("runtimeTargets", out var runtimeTargets))
                            {
                                continue;
                            }

                            foreach (var runtimeDependency in runtimeTargets.EnumerateObject())
                            {
                                var sourceFileName = Path.Combine(depsDirectory, runtimeDependency.Name);

                                foreach (var architectureStore in architectureStores)
                                {
                                    var targetFileName = Path.Combine(architectureStore, packages.Name.ToLowerInvariant(), runtimeDependency.Name);
                                    var targetDirectory = Path.GetDirectoryName(targetFileName);
                                    Directory.CreateDirectory(targetDirectory);
                                    File.Copy(sourceFileName, targetFileName);
                                }
                            }
                        }
                    }
                }

                void RemoveDuplicatedLibraries(string depsJsonContent, IReadOnlyList<string> architectureStores)
                {
                    var duplicatedLibraries = new List<(string Name, string Version)> { };

                    foreach (var duplicatedLibrary in duplicatedLibraries)
                    {
                        if (depsJsonContent.Contains(duplicatedLibrary.Name.ToLower() + "/" + duplicatedLibrary.Version))
                        {
                            throw new NotSupportedException($"Cannot remove {duplicatedLibrary.Name.ToLower()}/{duplicatedLibrary.Version} folder. It is referenced in json file");
                        }

                        foreach (var architectureStore in architectureStores)
                        {
                            var directoryToBeRemoved = Path.Combine(architectureStore, duplicatedLibrary.Name.ToLower(), duplicatedLibrary.Version);

                            if (!Directory.Exists(directoryToBeRemoved))
                            {
                                throw new NotSupportedException($"Directory {directoryToBeRemoved} does not exists. Verify it.");
                            }

                            Directory.Delete(directoryToBeRemoved, true);
                        }
                    }
                }

                void RemoveOpenTelemetryAutoInstrumentationAdditionalDepsFromDepsFile(string depsJsonContent, AbsolutePath file)
                {
                    // Remove OpenTelemetry.Instrumentation.AutoInstrumentationAdditionalDeps entry from target section.
                    depsJsonContent = Regex.Replace(depsJsonContent,
                        "\"OpenTelemetry(.+)AutoInstrumentation.AdditionalDeps.dll(.+?)}," + Environment.NewLine + "(.+?)\"", "\"",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    // Remove OpenTelemetry.Instrumentation.AutoInstrumentationAdditionalDeps entry from library section and write to file.
                    depsJsonContent = Regex.Replace(depsJsonContent, "\"OpenTelemetry(.+?)}," + Environment.NewLine + "(.+?)\"", "\"",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    File.WriteAllText(file, depsJsonContent);
                }

                void RemoveFilesFromAdditionalDepsDirectory()
                {
                    AdditionalDepsDirectory.GlobFiles("**/*.dll", "**/*.pdb", "**/*.xml", "**/*.dylib", "**/*.so").ForEach(DeleteFile);
                    AdditionalDepsDirectory.GlobDirectories("**/runtimes").ForEach(DeleteDirectory);
                }

                void AddFrameworkRollForwardCopy(string runtime, string rollForwardRuntime, IReadOnlyList<string> architectureStores)
                {
                    foreach (var architectureStore in architectureStores)
                    {
                        var assemblyDirectories = Directory.GetDirectories(architectureStore);
                        foreach (var assemblyDirectory in assemblyDirectories)
                        {
                            var assemblyVersionDirectories = Directory.GetDirectories(assemblyDirectory);
                            if (assemblyVersionDirectories.Length != 1)
                            {
                                throw new InvalidOperationException(
                                    $"Expected exactly one directory under {assemblyDirectory} but found {assemblyVersionDirectories.Length} instead.");
                            }

                            var assemblyVersionDirectory = assemblyVersionDirectories[0];
                            var sourceDir = Path.Combine(assemblyVersionDirectory, "lib", runtime);
                            if (Directory.Exists(sourceDir))
                            {
                                var destDir = Path.Combine(assemblyVersionDirectory, "lib", rollForwardRuntime);
                                // Directory.CreateDirectory(destDir);
                                CopyDirectoryRecursively(sourceDir, destDir);
                            }
                        }
                    }
                }
            });

        string GetFolderRuntimeName(JsonDocument jsonDocument)
        {
            var runtimeName = jsonDocument.RootElement.GetProperty("runtimeTarget").GetProperty("name").GetString();
            var folderRuntimeName = runtimeName switch
            {
                ".NETCoreApp,Version=v6.0" => "net6.0",
                ".NETCoreApp,Version=v7.0" => "net7.0",
                _ => throw new ArgumentOutOfRangeException(nameof(runtimeName), runtimeName,
                    "This value is not supported. You have probably introduced new .NET version to AutoInstrumentation")
            };
            return folderRuntimeName;
        }
    };

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

    private AbsolutePath GetResultsDirectory(Project proj) => BuildDataDirectory / "results" / proj.Name;

    /// <summary>
    /// Bootstrapping tests require every single test to be run in a separate process
    /// so the tracer can be created from scratch for each of them.
    /// </summary>
    private void RunBootstrappingTests()
    {
        var project = Solution.GetProject(Projects.Tests.AutoInstrumentationBootstrappingTests);
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
                    .EnableNoRestore()
                    .EnableNoBuild()
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetProjectFile(project)
                    .SetFilter(AndFilter(TestNameFilter(), testName))
                    .SetProcessEnvironmentVariable("BOOSTRAPPING_TESTS", "true"));
            }
        }
    }

    private string MapToFolderOutput(TargetFramework targetFramework)
    {
        return targetFramework.ToString().StartsWith("net4") ? "netfx" : "net";
    }
}
