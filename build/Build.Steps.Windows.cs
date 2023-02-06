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
using static Nuke.Common.IO.FileSystemTasks;
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
                // Can't use dotnet msbuild, as needs to use the VS version of MSBuild
                MSBuild(s => s
                    .SetTargetPath(project)
                    .SetConfiguration(BuildConfiguration)
                    .DisableRestore()
                    .SetMaxCpuCount(null)
                    .CombineWith(platforms, (m, platform) => m
                        .SetTargetPlatform(platform)));
            }
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

            // Can't use dotnet msbuild, as needs to use the VS version of MSBuild
            MSBuild(s => s
                .SetTargetPath(Solution.GetNativeTestProject())
                .SetConfiguration(BuildConfiguration)
                .DisableRestore()
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

                CopyFileToDirectory(source, dest, FileExistsPolicy.Overwrite);
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
            var project = Solution.GetProject(Projects.Tests.AutoInstrumentationNativeTests);
            var workingDirectory = project.Directory / "bin" / BuildConfiguration.ToString() / Platform.ToString();
            var exePath = workingDirectory / $"{project.Name}.exe";
            var testExe = ToolResolver.GetLocalTool(exePath);

            testExe("--gtest_output=xml", workingDirectory: workingDirectory);
        });

    Target PublishIisTestApplications => _ => _
        .Unlisted()
        .After(CompileManagedTests)
        .OnlyWhenStatic(() => IsWin && Containers == ContainersWindows)
        .Executes(() =>
        {
            var aspNetProject = Solution.GetProject(Projects.Tests.Applications.AspNet);

            MSBuild(x => x
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatform(Platform)
                .SetProperty("DeployOnBuild", true)
                .SetMaxCpuCount(null)
                    .SetProperty("PublishProfile", aspNetProject.Directory / "Properties" / "PublishProfiles" / $"FolderProfile.{BuildConfiguration}.pubxml")
                    .SetTargetPath(aspNetProject));

            DockerBuild(x => x
                .SetPath(".")
                .SetBuildArg($"configuration={BuildConfiguration}", $"windowscontainer_version={WindowsContainerVersion}")
                .SetRm(true)
                .SetTag(Path.GetFileNameWithoutExtension(aspNetProject).Replace(".", "-").ToLowerInvariant())
                .SetProcessWorkingDirectory(aspNetProject.Directory)
            );
        });

    Target GenerateNetFxTransientDependencies => _ => _
        .Unlisted()
        .After(Restore)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var project = Solution.GetProject(Projects.AutoInstrumentation).GetMSBuildProject();
            var packages = Solution.Directory / "src" / "Directory.Packages.props";

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

            var deps = Generator.EnumerateDependencies(project.FullPath);
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
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var netFxAssembliesFolder = TracerHomeDirectory / MapToFolderOutput(TargetFramework.NET462);
            var installTool = Solution.GetProject(Projects.Tools.GacInstallTool);

            var output = DotNetRun(s => s
                .SetProjectFile(installTool)
                .SetConfiguration(BuildConfiguration)
                .SetApplicationArguments(netFxAssembliesFolder));
        });
}
