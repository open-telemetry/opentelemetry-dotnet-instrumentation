using System;
using System.Collections.Generic;
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
                    var builder = Sdk.CreateTracerProviderBuilder()
                                     .UseEnvironmentVariables(settings)
                                     .AddSdkAspNetInstrumentation()
                                     .AddHttpClientInstrumentation()
                                     .AddSqlClientInstrumentation()
                                     .SetSampler(new AlwaysOnSampler())
                                     .AddSource("OpenTelemetry.AutoInstrumentation.*")
                                     .AddConsoleExporter();

                    _tracerProvider = builder.Build();
                }
            }
            catch (Exception)
            {
                // TODO: Should we have our own logger like datadog has?
            }
        }
    }
}
