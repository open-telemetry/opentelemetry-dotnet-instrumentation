using System;
using System.Collections.Generic;
using System.Linq;

namespace Samples.AspNet.Helpers;

public static class AssembliesHelper
{
    public static ICollection<string> GetLoadedTracesAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => x.FullName.StartsWith("OpenTelemetry"))
            .Select(x => x.FullName)
            .OrderBy(x => x)
            .ToList();
    }

    public static ICollection<string> GetLoadedAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(x => x.FullName)
            .OrderBy(x => x)
            .ToList();
    }
}