#if NETFRAMEWORK

using System;
using System.IO;
using System.Reflection;

namespace OpenTelemetry.ClrProfiler.Managed.Loader
{
    /// <summary>
    /// A class that attempts to load the OpenTelemetry.ClrProfiler.Managed .NET assembly.
    /// </summary>
    public partial class Startup
    {
        private static string ResolveManagedProfilerDirectory()
        {
            var tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;
            var tracerFrameworkDirectory = "net461";
            return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
        }

        private static Assembly AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name;

            // On .NET Framework, having a non-US locale can cause mscorlib
            // to enter the AssemblyResolve event when searching for resources
            // in its satellite assemblies. Exit early so we don't cause
            // infinite recursion.
            if (string.Equals(assemblyName, "mscorlib.resources", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = Path.Combine(ManagedProfilerDirectory, $"{assemblyName}.dll");
            if (File.Exists(path))
            {
                StartupLogger.Debug("Loading {0}", path);
                return Assembly.LoadFrom(path);
            }

            return null;
        }
    }
}

#endif
