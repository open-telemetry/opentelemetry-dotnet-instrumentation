using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;

partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildTracer);

    [Parameter("Configuration to build. Default is 'Release'")]
    readonly Configuration BuildConfiguration = Configuration.Release;

    [Parameter("Platform to build - x86 or x64. Default is 'x64'")]
    readonly MSBuildTargetPlatform Platform = MSBuildTargetPlatform.x64;

    [Parameter($"Docker containers type to be used. One of '{ContainersNone}', '{ContainersLinux}', '{ContainersWindows}'. Default is '{ContainersLinux}'")]
    readonly string Containers = ContainersLinux;

    const string ContainersNone = "none";
    const string ContainersLinux = "linux";
    const string ContainersWindows = "windows";

    [Parameter("Test projects filter. Optional, default matches all test projects. The project will be selected if the string is part of its name.")]
    readonly string TestProject = "";

    [Parameter("Test name fitler. Optional")]
    readonly string TestName;

    [Parameter("Number of times each dotnet test is run. Default is '1'")]
    readonly int TestCount = 1;

    [Parameter("Windows Server Core container version. Use it if your Windows does not support the default value. Default is 'ltsc2022'")]
    readonly string WindowsContainerVersion = "ltsc2022";

    [Parameter("The location to create the tracer home directory. Default is './bin/tracer-home'")]
    readonly AbsolutePath TracerHome;
    [Parameter("The location to place NuGet packages and other packages. Default is './bin/artifacts'")]
    readonly AbsolutePath Artifacts;

    [Parameter("The location to restore Nuget packages. Optional")]
    readonly AbsolutePath NugetPackageDirectory;

    Target Clean => _ => _
        .Description("Cleans all build output")
        .Executes(() =>
        {
            if (IsWin)
            {
                // These are created as part of the CreatePlatformlessSymlinks target and cause havok
                // when deleting directories otherwise
                DeleteReparsePoints(SourceDirectory);
                DeleteReparsePoints(TestsDirectory);
            }
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => DeleteDirectory(x));
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => DeleteDirectory(x));
            EnsureCleanDirectory(OutputDirectory);
            EnsureCleanDirectory(TracerHomeDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
            EnsureCleanDirectory(NativeProfilerProject.Directory / "build");
            EnsureCleanDirectory(NativeProfilerProject.Directory / "deps");
            EnsureCleanDirectory(BuildDataDirectory);

            void DeleteReparsePoints(string path)
            {
                new DirectoryInfo(path)
                   .GetDirectories("*", SearchOption.AllDirectories)
                   .Where(x => x.Attributes.HasFlag(FileAttributes.ReparsePoint))
                   .ForEach(dir => Cmd.Value(arguments: $"cmd /c rmdir \"{dir}\""));
            }
        });

    Target Workflow => _ => _
        .Description("GitHub workflow entry point")
        .DependsOn(Clean)
        .DependsOn(BuildTracer)
        .DependsOn(NativeTests)
        .DependsOn(ManagedTests);

    Target BuildTracer => _ => _
        .Description("Builds the native and managed src, and publishes the tracer home directory")
        .After(Clean)
        .DependsOn(CreateRequiredDirectories)
        .DependsOn(Restore)
        .DependsOn(CompileManagedSrc)
        .DependsOn(PublishManagedProfiler)
        .DependsOn(CompileNativeSrc)
        .DependsOn(PublishNativeProfiler)
        .DependsOn(CopyIntegrationsJson)
        .DependsOn(CopyInstrumentScripts);

    Target NativeTests => _ => _
        .Description("Builds the native unit tests and runs them")
        .After(Clean, BuildTracer)
        .DependsOn(CreateRequiredDirectories)
        .DependsOn(CompileNativeTests)
        .DependsOn(RunNativeTests);

    Target ManagedTests => _ => _
        .Description("Builds the managed unit / integration tests and runs them")
        .After(Clean, BuildTracer)
        .DependsOn(CreateRequiredDirectories)
        .DependsOn(CompileManagedTests)
        .DependsOn(CompileMocks)
        .DependsOn(PublishMocks)
        .DependsOn(PublishIisTestApplications)
        .DependsOn(RunManagedTests);

    string ContainersFilter()
    {
        switch (Containers)
        {
            case ContainersNone:
                return "Containers!=Linux&Containers!=Windows";
            case ContainersLinux:
                return "Containers!=Windows";
            case ContainersWindows:
                return "Containers!=Linux";
            default:
                throw new InvalidOperationException($"Container={Containers} is not supported");
        }
    }

    string TestNameFilter()
    {
        if (string.IsNullOrEmpty(TestName))
        {
            return null;
        }

        return "FullyQualifiedName~" + TestName;
    }

    string AndFilter(params string[] args)
    {
        var result = string.Empty;
        var first = true;

        foreach (var arg in args)
        {
            if (string.IsNullOrEmpty(arg))
            {
                continue;
            }

            if (first)
            {
                result = arg;
                first = false;
                continue;
            }

            result += "&" + arg;
        }

        return result;
    }
}
