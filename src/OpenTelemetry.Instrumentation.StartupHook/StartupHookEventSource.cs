using System.Diagnostics.Tracing;

namespace OpenTelemetry.Instrumentation.StartupHook
{
    /// <summary>
    /// EventSource implementation for OpenTelemetry StartupHook implementation.
    /// </summary>
    [EventSource(Name = "OpenTelemetry-Instrumentation-StartupHook")]
    internal class StartupHookEventSource : EventSource
    {
#pragma warning disable SA1401 // Fields should be private
        public static StartupHookEventSource Log = new StartupHookEventSource();
#pragma warning restore SA1401 // Fields should be private

        /// <summary>Logs as Trace level message.</summary>
        /// <param name="message">Message to log.</param>
        [Event(2, Message = "{0}", Level = EventLevel.Informational)]
        public void Trace(string message)
        {
            this.WriteEvent(1, message);
        }

        /// <summary>Logs as Error level message.</summary>
        /// <param name="message">Error to log.</param>
        [Event(2, Message = "{0}", Level = EventLevel.Error)]
        public void Error(string message)
        {
            this.WriteEvent(2, message);
        }
    }
}
