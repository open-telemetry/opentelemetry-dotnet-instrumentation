using System;
using System.Text;
using Datadog.Trace.Logging;
using Datadog.Trace.Vendors.Serilog.Events;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// Provides instrumentation probes that can be injected into profiled code.
    /// </summary>
    public static class Instrumentation
    {
        /// <summary>
        /// Gets the CLSID for the Datadog .NET profiler
        /// </summary>
        public static readonly string ProfilerClsid = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}";

        private static readonly Vendors.Serilog.ILogger Log = DatadogLogging.GetLogger(typeof(Instrumentation));

        /// <summary>
        /// Gets a value indicating whether Datadog's profiler is attached to the current process.
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

        /// <summary>
        /// Initializes global instrumentation values.
        /// </summary>
        public static void Initialize()
        {
            try
            {
                var tracer = Tracer.Instance;

                if (tracer.Settings.DiagnosticSourceEnabled)
                {
                    tracer.StartDiagnosticObservers();
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
