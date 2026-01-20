using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;

partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.BuildTracer);

    [Parameter("Configuration to build. Default is 'Release'")]
    readonly Configuration BuildConfiguration = Configuration.Release;

    [Parameter("Platform to build - x86, x64, ARM64. Default is current architecture.")]
    readonly MSBuildTargetPlatform Platform = GetDefaultTargetPlatform();

    [Parameter($"Docker containers type to be used in tests. One of '{ContainersNone}', '{ContainersLinux}', '{ContainersWindows}', '{ContainersWindowsOnly}'. Default is '{ContainersLinux}'")]
    readonly string Containers = ContainersLinux;

    [Parameter("TargetFramework to be tested. Default is empty, meaning all TFMs supported by each test")]
    readonly TargetFramework TestTargetFramework = TargetFramework.NOT_SPECIFIED;

    const string ContainersNone = "none";
    const string ContainersAny = "any";
    const string ContainersLinux = "linux";
    const string ContainersWindows = "windows";
    const string ContainersWindowsOnly = "windows-only";

    [Parameter("Test projects filter. Optional, default matches all test projects. The project will be selected if the string is part of its name.")]
    readonly string TestProject = "";

    [Parameter("Test name filter. Optional")]
    readonly string TestName;

    [Parameter("Number of times each dotnet test is run. Default is '1'")]
    readonly int TestCount = 1;

    [Parameter("The location to create the tracer home directory. Default is './bin/tracer-home'")]
    readonly AbsolutePath TracerHome;

    [Parameter("The location to place the NuGet packages built from the project. Default is './bin/nuget-artifacts'")]
    readonly AbsolutePath NuGetArtifacts;

    [Parameter("The location to restore NuGet packages. Optional")]
    readonly AbsolutePath NuGetPackagesDirectory;

    [Parameter("Do not restore the projects before building.")]
    readonly bool NoRestore;

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
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(x => x.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
            TracerHomeDirectory.CreateOrCleanDirectory();
            NuGetArtifactsDirectory.CreateOrCleanDirectory();
            (NativeProfilerProject.Directory / "build").CreateOrCleanDirectory();
            (NativeProfilerProject.Directory / "deps").CreateOrCleanDirectory();
            TestArtifactsDirectory.CreateOrCleanDirectory();

            void DeleteReparsePoints(string path)
            {
                new DirectoryInfo(path)
                   .GetDirectories("*", SearchOption.AllDirectories)
                   .Where(x => x.Attributes.HasFlag(FileAttributes.ReparsePoint))
                   .ForEach(dir => Cmd.Value(arguments: $"cmd /c rmdir \"{dir}\""));
            }
        });

    Target Workflow => _ => _
        .Description("Full workflow including build of deliverables (except NuGet packages) and run the respective tests")
        .DependsOn(BuildWorkflow)
        .DependsOn(TestWorkflow);

    Target BuildWorkflow => _ => _
        .Description("Builds the project deliverables (except NuGet packages)")
        .DependsOn(BuildTracer)
        .DependsOn(CompileExamples);

    Target BuildNativeWorkflow => _ => _
        .Description("Builds the native code project deliverables.")
        .After(Clean)
        .DependsOn(CreateRequiredDirectories)
        .DependsOn(CompileNativeSrc)
        .DependsOn(PublishNativeProfiler);

    Target TestWorkflow => _ => _
        .Description("Builds and run the tests against the local deliverables (except NuGet packages)")
        .After(BuildWorkflow)
        .DependsOn(NativeTests)
        .DependsOn(ManagedTests);

    Target BuildTracer => _ => _
        .Description("Builds the native and managed src, and publishes the tracer home directory")
        .After(Clean)
        .After(Restore)
        .DependsOn(CreateRequiredDirectories)
        .DependsOn(BuildInstallationScripts)
        .DependsOn(GenerateTransientDependencies)
        .DependsOn(CompileManagedSrc)
        .DependsOn(PublishManagedProfiler)
        .DependsOn(PublishRuleEngineJson)
        .DependsOn(GenerateAssemblyRedirectionSource)
        .DependsOn(CompileNativeSrc)
        .DependsOn(PublishNativeProfiler)
        .DependsOn(CopyInstrumentScripts)
        .DependsOn(CopyLegalFiles)
        .DependsOn(CreateVersionFile);

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
        .DependsOn(BuildInstallationScripts)
        .DependsOn(GenerateLibraryVersionFiles)
        .DependsOn(CompileNativeDependenciesForManagedTests)
        .DependsOn(CompileManagedTests)
        .DependsOn(CompileMocks)
        .DependsOn(PublishMocks)
        .DependsOn(PublishIisTestApplications)
        .DependsOn(InstallNetFxAssembliesGAC)
        .DependsOn(RunManagedTests);

    string ContainersFilter()
    {
        switch (Containers)
        {
            case ContainersNone:
                return "Containers!=Linux&Containers!=Windows&Containers!=Any";
            case ContainersLinux:
                return "Containers!=Windows";
            case ContainersWindows:
                return "Containers!=Linux";
            case ContainersWindowsOnly:
                return "Containers=Windows";
            case ContainersAny:
                throw new InvalidOperationException($"Containers={ContainersAny} is not supported directly. Specify concrete value, see help for options.");
            default:
                throw new InvalidOperationException($"Containers={Containers} is not supported");
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
}
