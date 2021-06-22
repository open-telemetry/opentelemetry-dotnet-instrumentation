#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed
{
    /// <summary>
    /// A class that attempts to load the OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed .NET assembly.
    /// </summary>
    public partial class Startup
    {
        private static IEnumerable<string> ResolveManagedProfilerDirectory()
        {
           // We currently build two assemblies targeting .NET Framework.
            // If we're running on the .NET Framework, load the highest-compatible assembly
            string corlibFileVersionString = ((AssemblyFileVersionAttribute)typeof(object).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
            string corlib461FileVersionString = "4.6.1055.0";

            // This will throw an exception if the version number does not match the expected 2-4 part version number of non-negative int32 numbers,
            // but mscorlib should be versioned correctly
            var corlibVersion = new Version(corlibFileVersionString);
            var corlib461Version = new Version(corlib461FileVersionString);
            var tracerFrameworkDirectory = corlibVersion < corlib461Version ? "net452" : "net461";

            var additionalDirectories = Environment.GetEnvironmentVariable("OTEL_DOTNET_TRACER_ADDITIONAL_ASSEMBLIES_DIRECTORIES")?.Split(Path.PathSeparator);
            if (additionalDirectories != null)
            {
                foreach (var path in additionalDirectories)
                {
                    yield return Path.Combine(path, tracerFrameworkDirectory);
                }
            }
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

            foreach (var directory in ManagedProfilerDirectories)
            {
                var path = Path.Combine(directory, $"{assemblyName}.dll");
                if (File.Exists(path))
                {
                    return Assembly.LoadFrom(path);
                }
            }

            return null;
        }
    }
}

#endif
