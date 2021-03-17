namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Names of HTTP headers that are commonly or internaly used
    /// </summary>
    public class CommonHttpHeaderNames
    {
        /// <summary>
        /// If header is set to "false", tracing is disabled for that http request.
        /// Tracing is enabled by default.
        /// </summary>
        public const string TracingEnabled = "otel-tracing-enabled";
    }
}
