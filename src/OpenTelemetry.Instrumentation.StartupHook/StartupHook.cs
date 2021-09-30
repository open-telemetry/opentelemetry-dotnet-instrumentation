using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using OpenTelemetry.Instrumentation.StartupHook;

/// <summary>
/// Dotnet StartupHook
/// </summary>
public class StartupHook
{
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
            string otelManagedProfilerFilePath = Path.Combine(AssemblyResolver.OtelManagedProfilerPath, "OpenTelemetry.ClrProfiler.Managed.dll");
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
            StartupHookEventSource.Log.Error($"Error in StartupHook initialization: ProfilerFolderLocation: {AssemblyResolver.OtelManagedProfilerPath}, Error: {ex}");
        }
    }
}
