using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class TargetFrameworksExtensions
    {
        public static IEnumerable<TargetFramework> FilterWindowsOnly(this IEnumerable<TargetFramework> frameworks)
        {
            return frameworks.Except(TargetFramework.WindowsOnly);
        }

        public static IEnumerable<TargetFramework> FilterNetStandard(this IEnumerable<TargetFramework> frameworks)
        {
            return frameworks.Except(TargetFramework.NetStandard);
        }

        public static IEnumerable<TargetFramework> CrossPlatformTestable(this IEnumerable<TargetFramework> frameworks)
        {
            return frameworks
                .FilterWindowsOnly()
                .FilterNetStandard();
        }
    }
}
