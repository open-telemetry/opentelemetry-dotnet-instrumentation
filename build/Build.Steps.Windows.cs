using System.Collections.Immutable;
using DependencyListGenerator;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.Docker.DockerTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

partial class Build
{
    Target CompileNativeSrcWindows => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .After(GenerateNetFxAssemblyRedirectionSource)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            // If we're building for x64, build for x86 too
            var platforms =
            Equals(Platform, MSBuildTargetPlatform.x64)
                ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
                : new[] { MSBuildTargetPlatform.x86 };

            foreach (var project in Solution.GetNativeSrcProjects())
            {
                PerformLegacyRestoreIfNeeded(project);

                var (major, minor, patch) = VersionHelper.GetVersionParts();

                // Can't use dotnet msbuild, as needs to use the VS version of MSBuild
                MSBuild(s => s
                    .SetTargetPath(project)
                    .SetConfiguration(BuildConfiguration)
                    .SetRestore(!NoRestore)
                    .SetMaxCpuCount(null)
                    .SetProperty("OTEL_AUTO_VERSION_MAJOR", major)
                    .SetProperty("OTEL_AUTO_VERSION_MINOR", minor)
                    .SetProperty("OTEL_AUTO_VERSION_PATCH", patch)
                    .CombineWith(platforms, (m, platform) => m
                        .SetTargetPlatform(platform)));
            }
        });

    Target CompileNativeDependenciesForManagedTestsWindows => _ => _
        .Unlisted()
        .After(CompileManagedSrc)
        .After(GenerateNetFxAssemblyRedirectionSource)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var continuousProfilerNativeDepProject = Solution.GetContinuousProfilerNativeDep();
            PerformLegacyRestoreIfNeeded(continuousProfilerNativeDepProject);
            // Can't use dotnet msbuild, as needs to use the VS version of MSBuild
            MSBuild(s => s
                .SetProjectFile(continuousProfilerNativeDepProject)
                .SetConfiguration(BuildConfiguration)
                .SetRestore(!NoRestore)
                .SetTargetPlatform(Platform)
                .SetRestore(false));
        });

    Target CompileNativeTestsWindows => _ => _
        .Unlisted()
        .After(CompileNativeSrc)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            // If we're building for x64, build for x86 too
            var platforms =
            Equals(Platform, MSBuildTargetPlatform.x64)
                ? new[] { MSBuildTargetPlatform.x64, MSBuildTargetPlatform.x86 }
                : new[] { MSBuildTargetPlatform.x86 };

            var nativeTestProject = Solution.GetNativeTestProject();
            PerformLegacyRestoreIfNeeded(nativeTestProject);

            // Can't use dotnet msbuild, as needs to use the VS version of MSBuild
            MSBuild(s => s
                .SetTargetPath(nativeTestProject)
                .SetConfiguration(BuildConfiguration)
                .SetRestore(!NoRestore)
                .SetMaxCpuCount(null)
                .CombineWith(platforms, (m, platform) => m
                    .SetTargetPlatform(platform)));
        });

    Target PublishNativeProfilerWindows => _ => _
        .Unlisted()
        .OnlyWhenStatic(() => IsWin)
        .After(CompileNativeSrc, PublishManagedProfiler)
        .Executes(() =>
        {
            foreach (var architecture in ArchitecturesForPlatform)
            {
                var source = NativeProfilerProject.Directory / "bin" / BuildConfiguration / architecture.ToString() /
                             $"{NativeProfilerProject.Name}.dll";
                var dest = TracerHomeDirectory / $"win-{architecture}";

                Log.Information($"Copying '{source}' to '{dest}'");

                source.CopyToDirectory(dest, ExistsPolicy.FileOverwrite);
            }
        });

    Target RunNativeTestsWindows => _ => _
        .Unlisted()
        .After(CompileNativeSrcWindows)
        .After(CompileNativeTestsWindows)
        .After(PublishManagedProfiler)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var project = Solution.GetProjectByName(Projects.Tests.AutoInstrumentationNativeTests);
            var workingDirectory = project.Directory / "bin" / BuildConfiguration.ToString() / Platform.ToString();
            var exePath = workingDirectory / $"{project.Name}.exe";
            var envVars = new Dictionary<string, string>(){
                { "OTEL_DOTNET_AUTO_LOG_DIRECTORY", ProfilerTestLogs }
            };
            var testExe = ToolResolver.GetTool(exePath);

            testExe($"--gtest_output=xml", workingDirectory: workingDirectory, environmentVariables: envVars);
        });

    Target PublishIisTestApplications => _ => _
        .Unlisted()
        .After(CompileManagedTests)
        .After(BuildInstallationScripts)
        .OnlyWhenStatic(() => IsWin && (Containers == ContainersWindows || Containers == ContainersWindowsOnly))
        .Executes(() =>
        {
            var aspNetProject = Solution.GetProjectByName(Projects.Tests.Applications.AspNet);
            BuildDockerImage(aspNetProject);

            DockerBuild(x => x
                .SetPath(".")
                .SetFile(aspNetProject.Directory / "Classic.Dockerfile")
                .EnableRm()
                .SetTag(($"{Path.GetFileNameWithoutExtension(aspNetProject).Replace(".", "-")}-classic").ToLowerInvariant())
                .SetProcessWorkingDirectory(aspNetProject.Directory)
            );

            var wcfProject = Solution.GetProjectByName(Projects.Tests.Applications.WcfIis);
            BuildDockerImage(wcfProject);

            var owinProject = Solution.GetProjectByName(Projects.Tests.Applications.OwinIis);
            BuildDockerImage(owinProject);
        });

    void BuildDockerImage(Project project)
    {
        const string moduleName = "OpenTelemetry.DotNet.Auto.psm1";
        var sourceModulePath = InstallationScriptsDirectory / moduleName;
        var localBinDirectory = project.Directory / "bin";
        var localTracerZip = localBinDirectory / "tracer.zip";

        try
        {
            sourceModulePath.CopyToDirectory(localBinDirectory);
            TracerHomeDirectory.ZipTo(localTracerZip);

            PerformLegacyRestoreIfNeeded(project);

            MSBuild(x => x
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatform(Platform)
                .SetProperty("DeployOnBuild", true)
                .SetMaxCpuCount(null)
                .SetProperty("PublishProfile",
                    project.Directory / "Properties" / "PublishProfiles" / $"FolderProfile.{BuildConfiguration}.pubxml")
                .SetTargetPath(project));

            DockerBuild(x => x
                .SetPath(".")
                .SetBuildArg($"configuration={BuildConfiguration}", $"windowscontainer_version={WindowsContainerVersion}")
                .EnableRm()
                .SetTag(Path.GetFileNameWithoutExtension(project).Replace(".", "-").ToLowerInvariant())
                .SetProcessWorkingDirectory(project.Directory)
            );
        }
        finally
        {
            localTracerZip.DeleteFile();
            var localModulePath = localBinDirectory / moduleName;
            localModulePath.DeleteFile();
        }
    }

    Target GenerateNetFxTransientDependencies => _ => _
        .Unlisted()
        .After(Restore)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            // The target project needs to have its NuGet packages restored prior to running the tool.
            var targetProject = Solution.GetProjectByName(Projects.AutoInstrumentation);
            DotNetRestore(s => s.SetProjectFile(targetProject));

            var project = targetProject.GetMSBuildProject();
            var packages = Solution.Directory / "src" / "Directory.Packages.props";
            var commonExcludedAssets = Solution.Directory / "src" / "CommonExcludedAssets.props";

            const string label = $"Transient dependencies auto-generated by {nameof(GenerateNetFxTransientDependencies)}";

            var packagesGroup = project.Xml.ItemGroups.FirstOrDefault(x => x.Label == label);
            if (packagesGroup == null)
            {
                packagesGroup = project.Xml.AddItemGroup();
                packagesGroup.Label = label;
                packagesGroup.Condition = " '$(TargetFramework)' == 'net462' ";
            }

            var packagesProject = ProjectModelTasks.ParseProject(packages);
            var versionGroup = packagesProject.Xml.ItemGroups.FirstOrDefault(x => x.Label == label);
            if (versionGroup == null)
            {
                versionGroup = packagesProject.Xml.AddItemGroup();
                versionGroup.Label = label;
            }

            var commonExcludedAssetsProject = ProjectModelTasks.ParseProject(commonExcludedAssets);
            var excludedProjectsGroup = commonExcludedAssetsProject.Xml.ItemGroups.First(x => x.Condition == string.Empty);

            var excludedDependencies = excludedProjectsGroup.Items.Select(x => x.Include).ToImmutableList();

            var deps = Generator.EnumerateDependencies(project.FullPath, excludedDependencies);
            foreach (var item in deps)
            {
                if (!packagesGroup.Items.Any(x => x.Include == item.Name))
                {
                    packagesGroup.AddItem("PackageReference", item.Name);
                }

                if (!versionGroup.Items.Any(x => x.Include == item.Name))
                {
                    var reference = versionGroup.AddItem("PackageVersion", item.Name);
                    reference.AddMetadata("Version", item.Version, expressAsAttribute: true);
                }
            }

            project.Save();
            packagesProject.Save();
        });

    Target GenerateNetFxAssemblyRedirectionSource => _ => _
        .Unlisted()
        .After(PublishManagedProfiler)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var netFxAssembliesFolder = TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET462);
            var generatedSourceFile = SourceDirectory / Projects.AutoInstrumentationNative / "netfx_assembly_redirection.h";

            AssemblyRedirectionSourceGenerator.Generate(netFxAssembliesFolder, generatedSourceFile);
        });

    Target InstallNetFxAssembliesGAC => _ => _
        .Unlisted()
        .After(BuildTracer)
        .After(RunManagedUnitTests)
        .OnlyWhenStatic(() => IsWin && (TestTargetFramework == TargetFramework.NET462 || TestTargetFramework == TargetFramework.NOT_SPECIFIED))
        .Executes(() => RunNetFxGacOperation("-i"));

    /// <remarks>
    /// Warning: This target could cause potential harm to your system by removing a required library from GAC.
    /// </remarks>
    Target UninstallNetFxAssembliesGAC => _ => _
        .Description("Removes .NET Framework output libraries from the GAC.")
        .After(BuildTracer)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() => RunNetFxGacOperation("-u"));

    private void RunNetFxGacOperation(string operation)
    {
        // To update the GAC, we need to run the tool as Administrator.
        // Throw if not running as a Windows Administrator.
        if (!IsWindowsAdministrator())
        {
            throw new InvalidOperationException("This target must be run on Windows as Administrator.");
        }

        var netFxAssembliesFolder = TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET462);
        var installTool = Solution.GetProjectByName(Projects.Tools.GacInstallTool);

        DotNetRun(s => s
            .SetProjectFile(installTool)
            .SetConfiguration(BuildConfiguration)
            .SetApplicationArguments(operation, netFxAssembliesFolder));

        static bool IsWindowsAdministrator()
        {
            if (!IsWin)
            {
                return false;
            }

#pragma warning disable CA1416 // Validate platform compatibility
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);

            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }

    private void PerformLegacyRestoreIfNeeded(Project project)
    {
        if (!NoRestore && project.Directory.ContainsFile("packages.config"))
        {
            RestoreLegacyNuGetPackagesConfig(new[] { project });
        }
    }
}
