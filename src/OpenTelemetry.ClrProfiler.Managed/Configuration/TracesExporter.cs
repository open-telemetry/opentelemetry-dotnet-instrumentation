namespace OpenTelemetry.ClrProfiler.Managed.Configuration
{
    /// <summary>
    /// Enum representing supported trace exporters.
    /// </summary>
    public enum TracesExporter
    {
        /// <summary>
        /// None exporter.
        /// </summary>
        None,

        /// <summary>
        /// OTLP exporter.
        /// </summary>
        Otlp,

        /// <summary>
        /// Jaeger exporter.
        /// </summary>
        Jaeger,

        /// <summary>
        /// Zipkin exporter.
        /// </summary>
        Zipkin,
    }
}
