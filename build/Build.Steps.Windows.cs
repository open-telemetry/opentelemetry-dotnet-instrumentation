using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;
using Nuke.Common.Tools.MSBuild;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.Docker.DockerTasks;
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
}
