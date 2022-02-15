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
        /// <summary>
        /// Initializes static members of the <see cref="Startup"/> class.
        /// This method also attempts to load the OpenTelemetry.ClrProfiler.Managed .NET assembly.
        /// </summary>
        static Startup()
        {
            ManagedProfilerDirectory = ResolveManagedProfilerDirectory();

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, "Unable to register a callback to the CurrentDomain.AssemblyResolve event.");
            }

            TryLoadManagedAssembly();
        }

        internal static string ManagedProfilerDirectory { get; }

        private static void TryLoadManagedAssembly()
        {
            StartupLogger.Log("Managed Loader TryLoadManagedAssembly()");

            try
            {
                var assembly = Assembly.Load("OpenTelemetry.ClrProfiler.Managed");
                if (assembly == null)
                {
                    throw new FileNotFoundException("The assembly OpenTelemetry.ClrProfiler.Managed could not be loaded");
                }

                var type = assembly.GetType("OpenTelemetry.ClrProfiler.Managed.Instrumentation", throwOnError: false);
                if (type == null)
                {
                    throw new TypeLoadException("The type OpenTelemetry.ClrProfiler.Managed.Instrumentation could not be loaded");
                }

                var method = type.GetRuntimeMethod("Initialize", new Type[0]);
                if (method == null)
                {
                    throw new MissingMethodException("The method OpenTelemetry.ClrProfiler.Managed.Instrumentation.Initialize could not be loaded");
                }

                method.Invoke(obj: null, parameters: null);
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, $"Error when loading managed assemblies. {ex.Message}");
                throw;
            }
        }

        private static string ReadEnvironmentVariable(string key)
        {
            try
            {
                return Environment.GetEnvironmentVariable(key);
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, "Error while loading environment variable " + key);
            }

            return null;
        }
    }
}
