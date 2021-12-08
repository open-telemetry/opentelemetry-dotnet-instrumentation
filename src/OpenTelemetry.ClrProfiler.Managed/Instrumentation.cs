using System;
using System.Diagnostics;
using System.Threading;
using OpenTelemetry.ClrProfiler.Managed.Configuration;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;

namespace OpenTelemetry.ClrProfiler.Managed
{
    /// <summary>
    /// Instrumentation
    /// </summary>
    public static class Instrumentation
    {
        private static readonly Process _process = Process.GetCurrentProcess();
        private static int _firstInitialization = 1;
        private static int _isExiting = 0;

        private static TracerProvider _tracerProvider;

        /// <summary>
        /// Gets a value indicating whether OpenTelemetry's profiler is attached to the current process.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the profiler is currently attached; <c>false</c> otherwise.
        /// </value>
        public static bool ProfilerAttached
        {
            get
            {
                try
                {
                    return NativeMethods.IsProfilerAttached();
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
            }
        }

        internal static Settings TracerSettings { get; } = Settings.FromDefaultSources();

        /// <summary>
        /// Initialize the OpenTelemetry SDK with a pre-defined set of exporters, shims, and
        /// instrumentations.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.Exchange(ref _firstInitialization, value: 0) != 1)
            {
                // Initialize() was already called before
                return;
            }

            try
            {
                if (TracerSettings.LoadTracerAtStartup)
                {
                    var builder = Sdk
                        .CreateTracerProviderBuilder()
                        .UseEnvironmentVariables(TracerSettings)
                        .SetSampler(new AlwaysOnSampler())
                        .InvokePlugins(TracerSettings.TracerPlugins);

                    _tracerProvider = builder.Build();
                    Log("OpenTelemetry tracer initialized.");

                    // Register to shutdown events
                    AppDomain.CurrentDomain.ProcessExit += OnExit;
                    AppDomain.CurrentDomain.DomainUnload += OnExit;
                }
            }
            catch (Exception ex)
            {
                Log($"OpenTelemetry SDK load exception: {ex}");
                throw;
            }

            try
            {
                if (_tracerProvider is not null)
                {
                    // Instantiate the OpenTracing shim. The underlying OpenTelemetry tracer will create
                    // spans using the "OpenTelemetry.ClrProfiler.OpenTracingShim" source.
                    var openTracingShim = new TracerShim(
                        _tracerProvider.GetTracer("OpenTelemetry.ClrProfiler.OpenTracingShim"),
                        Propagators.DefaultTextMapPropagator);

                    // This registration must occur prior to any reference to the OpenTracing tracer:
                    // otherwise the no-op tracer is going to be used by OpenTracing instead.
                    OpenTracing.Util.GlobalTracer.RegisterIfAbsent(openTracingShim);
                    Log("OpenTracingShim loaded.");
                }
                else
                {
                    Log("OpenTracingShim was not loaded as the provider is not initialized.");
                }
            }
            catch (Exception ex)
            {
                Log($"OpenTracingShim exception: {ex}");
                throw;
            }
        }

        private static void OnExit(object sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref _isExiting, value: 1) != 0)
            {
                // OnExit() was already called before
                return;
            }

            if (_tracerProvider is not null)
            {
                _tracerProvider.Dispose();
                _tracerProvider = null;

                Log("OpenTelemetry tracer exit.");
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> Process: {_process.ProcessName}({_process.Id}): {message}");
        }
    }
}
