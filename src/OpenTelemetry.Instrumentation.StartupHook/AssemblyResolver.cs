using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.Instrumentation.DotnetStartupHook
{
    internal class AssemblyResolver
    {
        public static Assembly LoadAssemblyFromSharedLocation(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string sharedAssemblyPath = null;
            try
            {
                // TODO: If dotnet does not like the provided assembly, it will retry this event in a loop.
                // Add a logic to avoid cyclic loop.
                sharedAssemblyPath = Path.Combine(StartupHook.StartupAssemblyLocation, $"{assemblyName.Name}.dll");
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
