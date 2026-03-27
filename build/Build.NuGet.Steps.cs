using System.Runtime.InteropServices;
using Extensions;
using Nuke.Common;
using Nuke.Common.IO;
#if NET10_0
using Nuke.Common.ProjectModel;
#endif
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    AbsolutePath NuGetArtifactsDirectory => NuGetArtifacts ?? (OutputDirectory / "nuget-artifacts");

    Target BuildNuGetPackages => _ => _
        .Description(
            "Builds the NuGet packages of the project assuming that any necessary build artifacts were already downloaded.")
        .DependsOn(CleanAutoInstrumentationNuGetPackagesFromLocalCaches)
        .DependsOn(BuildManagedSrcNuGetPackages)
        .DependsOn(SetupRuntimeNativeFolderForNuGetPackage)
        .DependsOn(BuildNuSpecNuGetPackages);

    Target TestNuGetPackages => _ => _
        .Description(
            "Test the NuGet packages of the project assuming that the packages are available at bin/nuget-artifacts.")
        .DependsOn(BuildNuGetPackagesTests)
        .DependsOn(BuildNuGetPackagesTestApplications)
        .DependsOn(RunNuGetPackagesTests);

    Target BuildManagedSrcNuGetPackages => _ => _
        .Description("Build the NuGet packages that are generated directly from src/**/*.csproj files")
        .Executes(() =>
        {
            foreach (var project in Solution.GetManagedSrcProjects().Where(p => !p.Name.EndsWith("Assemblies")))
            {
                DotNetPack(x => x
                    .SetProject(project)
                    .SetConfiguration(BuildConfiguration)
                    .SetOutputDirectory(NuGetArtifactsDirectory));
            }
        });

    Target SetupRuntimeNativeFolderForNuGetPackage => _ => _
        .Unlisted()
        .Description("Setup the \"runtimes/{platform}-{architecture}/native\" folders under \"nuget/OpenTelemetry.AutoInstrumentation.Runtime.Native\".")
        .Executes(() =>
        {
            var ciArtifactsDirectory = RootDirectory / "bin" / "ci-artifacts";
            var baseRuntimeNativePath = RootDirectory / "nuget" / "OpenTelemetry.AutoInstrumentation.Runtime.Native/";

            var requiredArtifacts = new string[]
            {
                "bin-alpine-x64/linux-musl-x64",
                "bin-alpine-arm64/linux-musl-arm64",
                "bin-ubuntu-22.04/linux-x64",
                "bin-ubuntu-22.04-arm/linux-arm64",
                "bin-macos-14/osx-arm64",
                "bin-windows-2022/win-x64",
                "bin-windows-2022/win-x86"
            };

            foreach (var artifactFolder in requiredArtifacts)
            {
                var sourcePath = ciArtifactsDirectory / artifactFolder;

                var platformAndArchitecture = Path.GetFileName(artifactFolder);
                var destinationPath = baseRuntimeNativePath / "runtimes" / platformAndArchitecture / "native";
                destinationPath.DeleteDirectory();

                sourcePath.Copy(destinationPath);
            }
        });

    Target CleanAutoInstrumentationNuGetPackagesFromLocalCaches => _ => _
        .Unlisted()
        .Description(
            "Remove the AutoInstrumentation packages from local caches ensuring that the latest locally built versions are used.")
        .Before(BuildManagedSrcNuGetPackages)
        .Before(BuildNuSpecNuGetPackages)
        .Executes(() =>
        {
            const string autoInstrumentationGlob = "opentelemetry.autoinstrumentation*"; // NuGet lowers the case of the directory.

            // This is mail fail if any dotnet tasks are using the packages on the background, reduce the risk by
            // shutting down any build servers. However, this doesn't prevent tools like VS and VS Code of holding
            // the BuildTasks package if they are doing background builds that reference the package.
            DotNet("dotnet build-server shutdown");

            // Clean the default local cache.
            var output = DotNet("dotnet nuget locals global-packages --list", RootDirectory);
            foreach (var line in output)
            {
                AbsolutePath packagesDir = Path.GetFullPath(line.Text[("global-packages: ".Length)..]);
                var autoInstrumentationPackagesDirectories = packagesDir.GlobDirectories(autoInstrumentationGlob);
                autoInstrumentationPackagesDirectories.ForEach(d => d.DeleteDirectory());
            }

            // Clean the NuGet test applications cache.
            var nugetTestAppsPackagesDir =
                RootDirectory / "test" / "test-applications" / "nuget-packages" / "packages";
            var nugetTestAppsAutoInstrumentationPackagesDirectories = nugetTestAppsPackagesDir.GlobDirectories(autoInstrumentationGlob);
            nugetTestAppsAutoInstrumentationPackagesDirectories.ForEach(d => d.DeleteDirectory());
        });

    Target BuildNuSpecNuGetPackages => _ => _
        .Description("Build the NuGet packages specified by nuget/**/*.nuspec projects.")
        .After(SetupRuntimeNativeFolderForNuGetPackage)
        .Executes(() =>
        {
            // .nuspec files don't support .props or another way to share properties.
            // To avoid repeating these values on all .nuspec files they are going to
            // be passed as properties.
            // Keeping common values here and using them as properties
            var nuspecCommonProperties = new Dictionary<string, object>
            {
                // NU5104: "A stable release of a package should not have a prerelease dependency."
                // NU5128: "Some target frameworks declared in the dependencies group of the nuspec and the lib/ref folder do not have exact matches in the other location."
                { "NoWarn", "NU5104;NU5128" },
                { "NuGetLicense", "Apache-2.0" },
                { "NuGetPackageVersion", VersionHelper.GetVersion() },
                { "NuGetRequiredLicenseAcceptance", "true" },
                { "OpenTelemetryAuthors", "OpenTelemetry Authors" },
                { "CommitId", VersionHelper.GetCommitId() }
            };

            var nuspecSolutionFolder = Solution.GetSolutionFolder("nuget")
                ?? throw new InvalidOperationException("Couldn't find the expected \"nuget\" solution folder.");

#if NET9_0
            throw new InvalidOperationException("NuSpec packaging requires .NET 10 or greater to run.");
#else
            var nuspecProjects = nuspecSolutionFolder.GetModel().Files!;

            foreach (var nuspecProject in nuspecProjects)
            {
                NuGetTasks.NuGetPack(s => s
                    .SetTargetPath(nuspecProject)
                    .SetConfiguration(BuildConfiguration)
                    .SetProperties(nuspecCommonProperties)
                    .SetOutputDirectory(NuGetArtifactsDirectory));
            }
#endif

        });

    Target BuildNuGetPackagesTests => _ => _
        .Description("Builds the NuGetPackagesTests project")
        .Executes(() =>
        {
            var nugetPackagesTestProject = Solution.GetProjectByName("NuGetPackagesTests");
            DotNetBuild(s => s
                .SetProjectFile(nugetPackagesTestProject)
                .SetConfiguration(BuildConfiguration));
        });

    Target BuildNuGetPackagesTestApplications => _ => _
        .Description("Builds the TestApplications.* used by the NuGetPackagesTests")
        .Executes(() =>
        {
            foreach (var packagesTestApplicationProject in Solution.GetNuGetPackagesTestApplications())
            {
                // Unlike the integration apps these require a restore step.
                DotNetBuild(s => s
                    .SetProjectFile(packagesTestApplicationProject)
                    .SetProperty("NuGetPackageVersion", VersionHelper.GetVersion())
                    .SetRuntime(RuntimeInformation.RuntimeIdentifier)
                    .SetSelfContained(true)
                    .SetConfiguration(BuildConfiguration)
                    .SetPlatform(Platform));

                // Build framework-dependent without specifying runtime identifier
                DotNetBuild(s => s
                    .SetProjectFile(packagesTestApplicationProject)
                    .SetProperty("NuGetPackageVersion", VersionHelper.GetVersion())
                    .SetConfiguration(BuildConfiguration)
                    .SetPlatform(Platform));
            }
        });

    Target RunNuGetPackagesTests => _ => _
        .Description("Run the NuGetPackagesTests.")
        .After(BuildNuGetPackagesTests)
        .After(BuildNuGetPackagesTestApplications)
        .Executes(() =>
        {
            var nugetPackagesTestProject = Solution.GetProjectByName("NuGetPackagesTests");

            for (var i = 0; i < TestCount; i++)
            {
                DotNetTest(config => config
                    .SetConfiguration(BuildConfiguration)
                    .SetFilter(AndFilter(TestNameFilter(), ContainersFilter()))
                    .SetBlameHangTimeout("5m")
                    .EnableTrxLogOutput(GetResultsDirectory(nugetPackagesTestProject))
                    .SetProjectFile(nugetPackagesTestProject)
                    .SetNoRestore(NoRestore)
                );
            }
        });
}
