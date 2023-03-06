using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

public static class ProjectsHelper
{
    private const string NativeProjectMarker = "Native"; // Contains word Native
    private const string TestsProjectMarker = "Tests"; // Ends with word Tests
    private const string NetFrameworkMarker = ".NetFramework"; // Ends with word .NetFramework

    private const string CoreProjectSelector = "OpenTelemetry.AutoInstrumentation*";
    private const string TestApplicationSelector = "TestApplication.*";
    private const string TestLibrarySelector = "TestLibrary.*";

    private readonly static AbsolutePath SrcDirectory = NukeBuild.RootDirectory / "src";
    private readonly static AbsolutePath TestDirectory = NukeBuild.RootDirectory / "test";

    public static IEnumerable<Project> GetManagedSrcProjects(this Solution solution)
    {
        return solution
            .GetProjects(CoreProjectSelector)
            .Where(x =>
                // Should contain in the src directory
                SrcDirectory.Contains(x.Directory) &&
                // Should not be native projects
                !x.Name.Contains(NativeProjectMarker));
    }

    public static IEnumerable<Project> GetNativeSrcProjects(this Solution solution)
    {
        return solution
            .GetProjects(CoreProjectSelector)
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
            .GetProjects(CoreProjectSelector)
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
        return solution.GetProject(Projects.Tests.IntegrationTests);
    }

    public static IEnumerable<Project> GetTestApplications(this Solution solution)
    {
        var testApplications = solution.GetProjects(TestApplicationSelector);
        var testLibraries = solution.GetProjects(TestLibrarySelector);

        return testApplications.Concat(testLibraries);
    }

    public static Project GetTestMock(this Solution solution)
    {
        return solution.GetProject(Projects.Mocks.AutoInstrumentationMock);
    }

    public static IEnumerable<Project> GetWindowsOnlyTestApplications(this Solution solution)
    {
        return solution
            .GetTestApplications()
            .Where(x => x.Name.EndsWith(NetFrameworkMarker));
    }

    public static IEnumerable<Project> GetCrossPlatformTestApplications(this Solution solution)
    {
        return solution
            .GetTestApplications()
            .Where(x => !x.Name.EndsWith(NetFrameworkMarker));
    }

    public static Project GetNativeTestProject(this Solution solution)
    {
        return solution.GetProject(Projects.Tests.AutoInstrumentationNativeTests);
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
}
