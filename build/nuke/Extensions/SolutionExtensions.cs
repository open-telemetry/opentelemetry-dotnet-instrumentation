using System;
using System.Linq;
using Nuke.Common.ProjectModel;

namespace Extensions
{
    public static class SolutionExtensions
    {
        public static Project[] GetIntegrationTests(this Solution solution)
        {
            return solution
                .GetProjects("IntegrationTests.*")
                // Skip helpers project
                .Where(x => !x.Name.Equals(Projects.Tests.IntegrationTestsHelpers, StringComparison.OrdinalIgnoreCase))
                // Skip .NET Fx integrations
                .Where(x => !x.Path.ToString().Contains("aspnet", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}
