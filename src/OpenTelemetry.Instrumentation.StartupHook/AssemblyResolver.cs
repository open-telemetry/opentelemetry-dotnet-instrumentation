using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.Instrumentation.StartupHook
{
    internal class AssemblyResolver
    {
        internal static string OtelManagedProfilerPath => Environment.GetEnvironmentVariable("OTEL_MANAGED_PROFILER_PATH");

        public static Assembly LoadAssemblyFromSharedLocation(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string sharedAssemblyPath = null;
            try
            {
                // TODO: If dotnet does not like the provided assembly, it will retry this event in a loop.
                // Add a logic to avoid cyclic loop.
                sharedAssemblyPath = Path.Combine(OtelManagedProfilerPath, $"{assemblyName.Name}.dll");
                if (File.Exists(sharedAssemblyPath))
                {
                    var assembly = Assembly.LoadFrom(sharedAssemblyPath);
                    return assembly;
                }
            }
            catch (Exception ex)
            {
                StartupHookEventSource.Log.Error($"Error Loading Assembly: {assemblyName.FullName}, location: {sharedAssemblyPath}, error:{ex}");
            }

            return null;
        }
    }
}
