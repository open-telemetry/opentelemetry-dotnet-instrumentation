using System;
using System.IO;
using System.Reflection;
using OpenTelemetry.Instrumentation.StartupHook;

/// <summary>
/// Dotnet StartupHook
/// </summary>
internal class StartupHook
{
    /// <summary>
    /// Load and initialize OpenTelemetry.ClrProfiler.Managed assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        string loaderAssemblyLocation = GetLoaderAssemblyLocation();

        try
        {
            // Check Instrumentation is already initialized with native profiler.
            Type profilerType = Type.GetType("OpenTelemetry.ClrProfiler.Managed.Instrumentation, OpenTelemetry.ClrProfiler.Managed");

            if (profilerType == null)
            {
                // Instrumentation is not initialized.
                // Creating an instance of OpenTelemetry.ClrProfiler.Managed.Loader.Startup
                // will initialize Instrumentation through its static constructor.
                string loaderFilePath = Path.Combine(loaderAssemblyLocation, "OpenTelemetry.ClrProfiler.Managed.Loader.dll");
                Assembly loaderAssembly = Assembly.LoadFrom(loaderFilePath);
                loaderAssembly.CreateInstance("OpenTelemetry.ClrProfiler.Managed.Loader.Startup");
                StartupHookEventSource.Log.Trace("StartupHook initialized successfully!");
            }
            else
            {
                StartupHookEventSource.Log.Trace("OpenTelemetry.ClrProfiler.Managed.Instrumentation initialized before startup hook");
            }
        }
        catch (Exception ex)
        {
            StartupHookEventSource.Log.Error($"Error in StartupHook initialization: LoaderFolderLocation: {loaderAssemblyLocation}, Error: {ex}");
            throw;
        }
    }

    private static string GetLoaderAssemblyLocation()
    {
        try
        {
            var startupAssemblyFilePath = Assembly.GetExecutingAssembly().Location;
            if (startupAssemblyFilePath.StartsWith(@"\\?\"))
            {
                // This will only be used in case the local path exceeds max_path size limit
                startupAssemblyFilePath = startupAssemblyFilePath.Substring(4);
            }

            // StartupHook and Loader assemblies are in the same path
            var startupAssemblyDirectoryPath = Path.GetDirectoryName(startupAssemblyFilePath) ??
                                               throw new NullReferenceException("StartupAssemblyFilePath is NULL");
            return startupAssemblyDirectoryPath;
        }
        catch (Exception ex)
        {
            StartupHookEventSource.Log.Error($"Error getting loader directory location: {ex}");
            throw;
        }
    }
}
