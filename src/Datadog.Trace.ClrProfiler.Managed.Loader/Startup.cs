using System;
using System.Reflection;

namespace Datadog.Trace.ClrProfiler.Managed.Loader
{
    /// <summary>
    /// A class that attempts to load the Datadog.Trace.ClrProfiler.Managed .NET assembly.
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        /// Initializes static members of the <see cref="Startup"/> class.
        /// This method also attempts to load the Datadog.Trace.ClrProfiler.Managed .NET assembly.
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
            try
            {
                var assembly = Assembly.Load("Datadog.Trace.ClrProfiler.Managed, Version=1.20.0.0, Culture=neutral, PublicKeyToken=def86d061d0d2eeb");

                if (assembly != null)
                {
                    // call method Datadog.Trace.ClrProfiler.Instrumentation.Initialize()
                    var type = assembly.GetType("Datadog.Trace.ClrProfiler.Instrumentation", throwOnError: false);
                    var method = type?.GetRuntimeMethod("Initialize", parameters: new Type[0]);
                    method?.Invoke(obj: null, parameters: null);
                }
            }
            catch (Exception ex)
            {
                StartupLogger.Log(ex, "Error when loading managed assemblies.");
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
