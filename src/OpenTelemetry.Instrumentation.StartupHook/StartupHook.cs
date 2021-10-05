using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.Instrumentation.DotnetStartupHook;

/// <summary>
/// Dotnet StartupHook
/// </summary>
public class StartupHook
{
#pragma warning disable SA1401 // Fields should be private
    internal static string StartupAssemblyLocation = GetStartupAssemblyLocation();
#pragma warning restore SA1401 // Fields should be private

    /// <summary>
    /// Load and initialize OpenTelemetry.ClrProfiler.Managed assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += AssemblyResolver.LoadAssemblyFromSharedLocation;
        InitializeInstrumentation();
    }

    private static void InitializeInstrumentation()
    {
        try
        {
            // Load OpenTelemetry.ClrProfiler.Managed assembly
            string otelManagedProfilerFilePath = Path.Combine(StartupAssemblyLocation, "OpenTelemetry.ClrProfiler.Managed.dll");
            Assembly otelManagedProfilerAssembly = Assembly.LoadFrom(otelManagedProfilerFilePath);

            // Call Instrumentation.Initialize()
            Type otelProfilerInstrumentationType = otelManagedProfilerAssembly?.GetType("OpenTelemetry.ClrProfiler.Managed.Instrumentation");
            MethodInfo otelProfilerInitializeMethodInfo = otelProfilerInstrumentationType?.GetMethod("Initialize");

            otelProfilerInitializeMethodInfo?.Invoke(null, null);

            if (otelProfilerInitializeMethodInfo != null)
            {
                StartupHookEventSource.Log.Trace("StartupHook initialized successfully!");
            }
            else
            {
                StartupHookEventSource.Log.Error("StartupHook initialization has failed!");
            }
        }
        catch (Exception ex)
        {
            StartupHookEventSource.Log.Error($"Error in StartupHook initialization: ProfilerFolderLocation: {StartupAssemblyLocation}, Error: {ex}");
        }
    }

    private static string GetStartupAssemblyLocation()
    {
        try
        {
            var startupAssemblyFilePath = Assembly.GetExecutingAssembly().Location;
            if (startupAssemblyFilePath.StartsWith(@"\\?\"))
            {
                startupAssemblyFilePath = startupAssemblyFilePath.Substring(4); // This will only be used in case the local path exceeds max_path size limit
            }

            var startupAssemblyDirectoryPath = Path.GetDirectoryName(startupAssemblyFilePath);
            return startupAssemblyDirectoryPath;
        }
        catch (Exception ex)
        {
            StartupHookEventSource.Log.Error($"Error getting startup folder location: {ex}");
            return null;
        }
    }
}
