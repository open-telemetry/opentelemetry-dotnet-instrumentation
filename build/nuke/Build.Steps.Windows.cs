using System.IO;
using Extensions;
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
                .SetTargetPath(MsBuildProject)
                .SetConfiguration(BuildConfiguration)
                .SetTargets("BuildCpp")
                .DisableRestore()
                .SetMaxCpuCount(null)
                .CombineWith(platforms, (m, platform) => m
                    .SetTargetPlatform(platform)));
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
                .SetTargetPath(MsBuildProject)
                .SetConfiguration(BuildConfiguration)
                .SetTargets("BuildCppTests")
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
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var project = Solution.GetProject(Projects.Tests.ClrProfilerNativeTests);
            var workingDirectory = project.Directory / "bin" / BuildConfiguration.ToString() / Platform.ToString();
            var exePath = workingDirectory / $"{project.Name}.exe";
            var testExe = ToolResolver.GetLocalTool(exePath);

            testExe("--gtest_output=xml", workingDirectory: workingDirectory);
        });

    Target CompileManagedTestsWindows => _ => _
        .Unlisted()
        .After(CompileManagedTests)
        .OnlyWhenStatic(() => IsWin)
        .Triggers(PublishIisSamples)
        .Executes(() =>
        {
            // Compile .NET Framework projects

            MSBuild(x => x
                .SetTargetPath(MsBuildProject)
                .SetTargetPlatform(Platform)
                .SetConfiguration(BuildConfiguration)
                .DisableRestore()
                .SetTargets("BuildCsharpFXTest")
            );
        });

    Target PublishIisSamples => _ => _
        .Unlisted()
        .After(CompileManagedTestsWindows)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            var aspnetFolder = TestsDirectory / "test-applications" / "integrations" / "aspnet";
            var aspnetProjects = aspnetFolder.GlobFiles("**/*.csproj");

            MSBuild(x => x
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatform(Platform)
                .SetProperty("DeployOnBuild", true)
                .SetMaxCpuCount(null)
                .CombineWith(aspnetProjects, (c, project) => c
                    .SetProperty("PublishProfile", project.Parent / "Properties" / "PublishProfiles" / $"FolderProfile.{BuildConfiguration}.pubxml")
                    .SetTargetPath(project)));

            foreach (var proj in aspnetProjects)
            {
                DockerBuild(x => x
                    .SetPath(".")
                    .SetBuildArg($"configuration={BuildConfiguration}")
                    .SetRm(true)
                    .SetTag(Path.GetFileNameWithoutExtension(proj).Replace(".", "-").ToLowerInvariant())
                    .SetProcessWorkingDirectory(proj.Parent)
                );
            }
        });

    Target RunManagedTestsWindows => _ => _
        .Unlisted()
        .After(RunManagedTests)
        .DependsOn(CompileManagedTestsWindows)
        .DependsOn(PublishIisSamples)
        .OnlyWhenStatic(() => IsWin)
        .Executes(() =>
        {
            Project[] aspNetTests = Solution.GetWindowsOnlyIntegrationTests();

            DotNetTest(config => config
                .SetConfiguration(BuildConfiguration)
                .SetTargetPlatform(Platform)
                .SetFramework(TargetFramework.NET461)
                .EnableNoRestore()
                .EnableNoBuild()
                .CombineWith(aspNetTests, (s, project) => s
                    .EnableTrxLogOutput(GetResultsDirectory(project))
                    .SetProjectFile(project)), degreeOfParallelism: 4);
        });
}
