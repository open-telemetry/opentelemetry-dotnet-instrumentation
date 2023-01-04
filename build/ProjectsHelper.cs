using System.Collections.Generic;
using System.Linq;
using Nuke.Common.ProjectModel;

public static class ProjectsHelper
{
    public static IEnumerable<Project> GetManagedSrcProjects(this Solution solution)
    {
        return solution
            .GetProjects("OpenTelemetry.AutoInstrumentation*")
            .Where(x =>
                // Should contain in the src directory
                x.Directory.ToString().Contains("src") &&
                // Should not be native projects
                !x.Name.Contains("Native"));
    }

    public static IEnumerable<Project> GetNativeSrcProjects(this Solution solution)
    {
        return solution
            .GetProjects("OpenTelemetry.AutoInstrumentation*")
            .Where(x =>
                // Should contain in the src directory
                x.Directory.ToString().Contains("src") &&
                // Should be native projects
                x.Name.Contains("Native"));
    }

    public static IEnumerable<Project> GetManagedTestProjects(this Solution solution)
    {
        return solution.GetManagedUnitTestProjects()
            .Concat(new[] { solution.GetManagedIntegrationTestProject() });
    }

    public static IEnumerable<Project> GetManagedUnitTestProjects(this Solution solution)
    {
        return solution
            .GetProjects("OpenTelemetry.AutoInstrumentation*")
            .Where(x =>
                // Should contain in the test directory
                x.Directory.ToString().Contains("test") &&
                // Should not be native projects
                !x.Name.Contains("Native") &&
                // Should be test projects
                x.Name.EndsWith("Tests"));
    }

    public static Project GetManagedIntegrationTestProject(this Solution solution)
    {
        return solution.GetProject("IntegrationTests");
    }

    public static IEnumerable<Project> GetTestApplications(this Solution solution)
    {
        var testApplications = solution.GetProjects("TestApplication.*");
        var testLibraries = solution.GetProjects("TestLibrary.*");

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
            .Where(x => x.Name.Contains("AspNet") || x.Name.EndsWith("NetFramework"));
    }

    public static IEnumerable<Project> GetCrossPlatformTestApplications(this Solution solution)
    {
        return solution
            .GetTestApplications()
            .Where(x => !x.Name.Contains("AspNet") && !x.Name.EndsWith("NetFramework"));
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
