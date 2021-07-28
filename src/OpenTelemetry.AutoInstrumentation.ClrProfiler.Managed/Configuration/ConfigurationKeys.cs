using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    /// <summary>
    /// Configuration keys
    /// </summary>
    public class ConfigurationKeys
    {
        /// <summary>
        /// Configuration key for the application's environment. Sets the "env" tag on every <see cref="Activity"/>.
        /// </summary>
        /// <seealso cref="Settings.Environment"/>
        public const string Environment = "OTEL_ENV";

        /// <summary>
        /// Configuration key for enabling or disabling the Tracer.
        /// Default is value is true (enabled).
        /// </summary>
        /// <seealso cref="Settings.TraceEnabled"/>
        public const string TraceEnabled = "OTEL_TRACE_ENABLED";

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
        /// Configuration key for whether the console exporter is enabled.
        /// </summary>
        public const string ConsoleExporterEnabled = "OTEL_DOTNET_TRACER_CONSOLE_EXPORTER_ENABLED";

        /// <summary>
        /// Configuration key for comma separated list of enabled instrumentations.
        /// </summary>
        public const string Instrumentations = "OTEL_DOTNET_TRACER_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for comma separated list of disabled instrumentations.
        /// </summary>
        public const string DisabledInstrumentations = "OTEL_DOTNET_TRACER_DISABLED_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for colon (:) separated list of plugins repesented by <see cref="System.Type.AssemblyQualifiedName"/>.
        /// </summary>
        public const string ProviderPlugins = "OTEL_DOTNET_TRACER_INSTRUMENTATION_PLUGINS";

        /// <summary>
        /// Configuration key for additional <see cref="ActivitySource"/> names to be added to the tracer at the startup.
        /// </summary>
        public const string AdditionalSources = "OTEL_DOTNET_TRACER_ADDITIONAL_SOURCES";

        /// <summary>
        /// Configuration key for legacy source names to be added to the tracer at the startup.
        /// </summary>
        public const string LegacySources = "OTEL_DOTNET_TRACER_LEGACY_SOURCES";

        /// <summary>
        /// Configuration key for a list of tags to be applied globally to spans.
        /// </summary>
        /// <seealso cref="Settings.GlobalTags"/>
        public const string GlobalTags = "OTEL_TAGS";

        /// <summary>
        /// String constants for debug configuration keys.
        /// </summary>
        internal static class Debug
        {
            /// <summary>
            /// Configuration key for forcing the automatic instrumentation to only use the mdToken method lookup mechanism.
            /// </summary>
            public const string ForceMdTokenLookup = "OTEL_TRACE_DEBUG_LOOKUP_MDTOKEN";

            /// <summary>
            /// Configuration key for forcing the automatic instrumentation to only use the fallback method lookup mechanism.
            /// </summary>
            public const string ForceFallbackLookup = "OTEL_TRACE_DEBUG_LOOKUP_FALLBACK";
        }

        /// <summary>
        /// String format patterns used to match integration-specific configuration keys.
        /// </summary>
        internal static class Integrations
        {
            /// <summary>
            /// Configuration key pattern for enabling or disabling an integration.
            /// </summary>
            public const string Enabled = "OTEL_TRACE_{0}_ENABLED";

            /// <summary>
            /// Configuration key pattern for enabling or disabling Analytics in an integration.
            /// </summary>
            public const string AnalyticsEnabled = "OTEL_TRACE_{0}_ANALYTICS_ENABLED";

            /// <summary>
            /// Configuration key pattern for setting Analytics sampling rate in an integration.
            /// </summary>
            public const string AnalyticsSampleRate = "OTEL_TRACE_{0}_ANALYTICS_SAMPLE_RATE";
        }
    }
}
