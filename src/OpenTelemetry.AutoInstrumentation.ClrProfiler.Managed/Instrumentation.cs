using System;
using System.Diagnostics;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Shims.OpenTracing;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed
{
    /// <summary>
    /// Instrumentation
    /// </summary>
    public static class Instrumentation
    {
        private static readonly Process _process = Process.GetCurrentProcess();
        private static int _firstInitialization = 1;

        private static TracerProvider _tracerProvider;

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
                }
            }
            catch (Exception ex)
            {
                Log($"OpenTelemetry SDK load exception: {ex}");
                throw;
            }

            try
            {
                // Instantiate the OpenTracing shim. The underlying OpenTelemetry tracer will create
                // spans using the "OpenTelemetry.AutoInstrumentation.OpenTracingShim" source.
                var openTracingShim = new TracerShim(
                    _tracerProvider.GetTracer("OpenTelemetry.AutoInstrumentation.OpenTracingShim"),
                    Propagators.DefaultTextMapPropagator);

                // This registration must occur prior to any reference to the OpenTracing tracer:
                // otherwise the no-op tracer is going to be used by OpenTracing instead.
                OpenTracing.Util.GlobalTracer.Register(openTracingShim);
                Log("OpenTracingShim loaded.");
            }
            catch (Exception ex)
            {
                Log($"OpenTracingShim exception: {ex}");
                throw;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> Process: {_process.ProcessName}({_process.Id}): {message}");
        }
    }
}
