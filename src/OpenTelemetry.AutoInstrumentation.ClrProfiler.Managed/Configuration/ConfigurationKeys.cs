namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    /// <summary>
    /// Configuration keys
    /// </summary>
    public class ConfigurationKeys
    {
        /// <summary>
        /// Configuration key for the application's default service name.
        /// Used as the service name for top-level spans,
        /// and used to determine service name of some child spans.
        /// </summary>
        public const string ServiceName = "OTEL_SERVICE";

        /// <summary>
        /// Configuration key for the application's version. Sets the "version" tag on every span.
        /// </summary>
        public const string ServiceVersion = "OTEL_VERSION";

        /// <summary>
        /// Configuration key for whether the tracer should be initialized by the profiler or not.
        /// </summary>
        public const string LoadTracerAtStartup = "OTEL_DOTNET_TRACER_LOAD_AT_STARTUP";

        /// <summary>
        /// Configuration key for the exporter to be used. The Tracer uses it to encode and
        /// dispatch traces.
        /// Default is <c>"Zipkin"</c>.
        /// </summary>
        public const string Exporter = "OTEL_EXPORTER";

        /// <summary>
        /// Configuration key for the Agent URL where the Tracer can send traces.
        /// Default value is "http://localhost:8126".
        /// </summary>
        public const string ZipkinEndpoint = "OTEL_EXPORTER_ZIPKIN_ENDPOINT";

        /// <summary>
        /// Configuration key for hostname for the Jaeger agent.
        /// </summary>
        public const string JaegerExporterAgentHost = "OTEL_EXPORTER_JAEGER_AGENT_HOST";

        /// <summary>
        /// Configuration key for port for the Jaeger agent.
        /// </summary>
        public const string JaegerExporterAgentPort = "OTEL_EXPORTER_JAEGER_AGENT_PORT";

        /// <summary>
        /// Configuration key for whether the console exporter is enabled.
        /// </summary>
        public const string ConsoleExporterEnabled = "OTEL_CONSOLE_EXPORTER_ENABLED";

        /// <summary>
        /// Configuration key for comma separated list of enabled instrumentations.
        /// </summary>
        public const string EnabledInstrumentations = "OTEL_ENABLED_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for additional activitysource names to be added to the tracer at the startup.
        /// </summary>
        public const string AdditionalSources = "OTEL_ADDITIONAL_SOURCES";
    }
}
