using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common.ProjectModel;

namespace Extensions
{
    public static class SolutionExtensions
    {
        public static Project[] GetCrossPlatformIntegrationTests(this Solution solution)
        {
            return GetIntegrationTests(solution)
                // Exclude Windows only tests
                .Where(x => !(x.GetProperty<bool?>("WindowsOnly") ?? false))
                .ToArray();
        }

        public static Project[] GetWindowsOnlyIntegrationTests(this Solution solution)
        {
            return GetIntegrationTests(solution)
            // Take Windows only tests
            .Where(x => x.GetProperty<bool?>("WindowsOnly") ?? false)
            .ToArray();
        }

        public static IEnumerable<Project> GetIntegrationTests(this Solution solution)
        {
            return solution
                .GetProjects("IntegrationTests.*");
        }
    }
}
