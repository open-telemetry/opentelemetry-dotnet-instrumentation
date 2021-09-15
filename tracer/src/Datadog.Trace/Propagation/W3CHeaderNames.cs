namespace Datadog.Trace.Propagation
{
    /// <summary>
    /// Names of HTTP headers that can be used tracing inbound or outbound HTTP requests.
    /// </summary>
    public static class W3CHeaderNames
    {
        /// <summary>
        /// Describes the position of the incoming request in its trace graph (format: 00-{trace-id}-{parent-id}-{trace-flags}).
        /// </summary>
        public const string TraceParent = "traceparent";

        /// <summary>
        /// Extends traceparent with vendor-specific data.
        /// </summary>
        public const string TraceState = "tracestate";
    }
}
