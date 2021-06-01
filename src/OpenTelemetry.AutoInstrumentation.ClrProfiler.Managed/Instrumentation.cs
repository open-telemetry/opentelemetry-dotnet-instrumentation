using System;
using System.Diagnostics;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed
{
    /// <summary>
    /// Instrumentation
    /// </summary>
    public static class Instrumentation
    {
        private static int _firstInitialization = 1;

        private static TracerProvider _tracerProvider;

        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize()
        {
            var p = Process.GetCurrentProcess();
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> Process: {p.ProcessName}({p.Id}), starting");
            if (Interlocked.Exchange(ref _firstInitialization, value: 0) != 1)
            {
                // Initialize() was already called before
                return;
            }

            try
            {
                var settings = Settings.Instance;
                if (settings.LoadTracerAtStartup)
                {
                    var builder = Sdk
                        .CreateTracerProviderBuilder()
                        .UseEnvironmentVariables(settings)
                        .SetSampler(new AlwaysOnSampler())
                        .AddSource("OpenTelemetry.AutoInstrumentation.*");

                    _tracerProvider = builder.Build();
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> Process: {p.ProcessName}({p.Id}), initialized");
                }
            }
            catch (Exception)
            {
                // TODO: Should we have our own logger like datadog has?
                throw;
            }
        }
    }
}
