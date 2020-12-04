namespace Datadog.Trace.Configuration
{
    /// <summary>
    /// Enumeration for the available exporter types.
    /// </summary>
    public enum ExporterType
    {
        /// <summary>
        /// The default exporter.
        /// </summary>
        Default,

        /// <summary>
        /// The Datadog Agent exporter.
        /// </summary>
        DatadogAgent = Default,

        /// <summary>
        /// The Zipkin exporter.
        /// </summary>
        Zipkin
    }
}
