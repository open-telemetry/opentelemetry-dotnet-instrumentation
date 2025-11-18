using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

public static class ProjectsHelper
{
    private const string NativeProjectMarker = "Native"; // Contains word Native
    private const string TestsProjectMarker = "Tests"; // Ends with word Tests
    private const string NetFrameworkMarker = ".NetFramework"; // Ends with word .NetFramework
    private const string NativeDepMarker = ".NativeDep"; // Ends with word .NativeDep

    private const string CoreProjectSelector = "OpenTelemetry.AutoInstrumentation*";
    private const string TestApplicationSelector = "TestApplication.*";
    private const string TestLibrarySelector = "TestLibrary.*";

    private static readonly AbsolutePath SrcDirectory = NukeBuild.RootDirectory / "src";
    private static readonly AbsolutePath TestDirectory = NukeBuild.RootDirectory / "test";
    private static readonly AbsolutePath TestIntegrationApps = TestDirectory / "test-applications" / "integrations";
    private static readonly AbsolutePath TestNuGetPackagesApps = TestDirectory / "test-applications" / "nuget-packages";

    public static IEnumerable<Project> GetManagedSrcProjects(this Solution solution)
    {
        return solution
            .GetAllProjects(CoreProjectSelector)
            .Where(x =>
                // Should contain in the src directory
                SrcDirectory.Contains(x.Directory) &&
                // Should not be native projects
                !x.Name.Contains(NativeProjectMarker)
                && (!x.Name.EndsWith(NetFrameworkMarker) || EnvironmentInfo.IsWin));
    }

    public static IEnumerable<Project> GetNativeSrcProjects(this Solution solution)
    {
        return solution
            .GetAllProjects(CoreProjectSelector)
            .Where(x =>
                // Should contain in the src directory
                SrcDirectory.Contains(x.Directory) &&
                // Should be native projects
                x.Name.Contains(NativeProjectMarker));
    }

    public static IEnumerable<Project> GetManagedTestProjects(this Solution solution)
    {
        return solution.GetManagedUnitTestProjects()
            .Concat(new[] { solution.GetManagedIntegrationTestProject() });
    }

    public static IEnumerable<Project> GetManagedUnitTestProjects(this Solution solution)
    {
        return solution
            .GetAllProjects(CoreProjectSelector)
            .Where(x =>
                // Should contain in the test directory
                TestDirectory.Contains(x.Directory) &&
                // Should not be native projects
                !x.Name.Contains(NativeProjectMarker) &&
                // Should be test projects
                x.Name.EndsWith(TestsProjectMarker));
    }

    public static Project GetManagedIntegrationTestProject(this Solution solution)
    {
        return solution.AllProjects.First(project => project.Name == Projects.Tests.IntegrationTests);
    }

    public static IEnumerable<Project> GetIntegrationTestApplications(this Solution solution)
    {
        var testApplications = solution
            .GetAllProjects(TestApplicationSelector)
            .Where(p => TestIntegrationApps.Contains(p.Directory));
        var testLibraries = solution.GetAllProjects(TestLibrarySelector);

        return testApplications.Concat(testLibraries);
    }

    public static IEnumerable<Project> GetNuGetPackagesTestApplications(this Solution solution)
    {
        return solution
            .GetAllProjects(TestApplicationSelector)
            .Where(p => TestNuGetPackagesApps.Contains(p.Directory));
    }

    public static Project GetTestMock(this Solution solution)
    {
        return solution.GetProjectByName(Projects.Mocks.AutoInstrumentationMock);
    }

    public static Project GetContinuousProfilerNativeDep(this Solution solution)
    {
        return solution.GetProjectByName(Projects.Tests.Applications.ContinuousProfilerNativeDep);
    }

    public static IEnumerable<Project> GetNetFrameworkOnlyTestApplications(this Solution solution)
    {
        return solution
            .GetIntegrationTestApplications()
            .Where(x => x.Name.EndsWith(NetFrameworkMarker));
    }

    public static IEnumerable<Project> GetCrossPlatformTestApplications(this Solution solution)
    {
        return solution
            .GetIntegrationTestApplications()
            .Where(x => !x.Name.EndsWith(NetFrameworkMarker))
            .Where(x => !x.Name.EndsWith(NativeDepMarker));
    }

    public static Project GetNativeTestProject(this Solution solution)
    {
        return solution.GetProjectByName(Projects.Tests.AutoInstrumentationNativeTests);
    }

    public static IEnumerable<Project> GetCrossPlatformManagedProjects(this Solution solution)
    {
        return solution.GetManagedSrcProjects()
            .Concat(solution.GetManagedTestProjects())
            .Concat(solution.GetCrossPlatformTestApplications())
            .Concat(new[] { solution.GetTestMock() });
    }

    public static IEnumerable<Project> GetNativeProjects(this Solution solution)
    {
        return solution.GetNativeSrcProjects()
            .Concat(new[] { solution.GetNativeTestProject() });
    }

    public static Project GetProjectByName(this Solution solution, string projectName)
    {
        return solution.AllProjects.First(projest => projest.Name == projectName);
    }
}
