// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Configuration keys
/// </summary>
internal partial class ConfigurationKeys
{
    /// <summary>
    /// Configuration key for enabling profiler.
    /// </summary>
#if NETFRAMEWORK
    public const string ProfilingEnabled = "COR_ENABLE_PROFILING";
#else
    public const string ProfilingEnabled = "CORECLR_ENABLE_PROFILING";
#endif

    /// <summary>
    /// Configuration key for the OTLP protocol to be used.
    /// Default is <c>"http/protobuf"</c>.
    /// </summary>
    public const string ExporterOtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>
    /// Configuration key for enabling the flushing of telemetry data when an unhandled exception occurs.
    /// </summary>
    public const string FlushOnUnhandledException = "OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION";

    /// <summary>
    /// Configuration key for colon (:) separated list of plugins represented by <see cref="System.Type.AssemblyQualifiedName"/>.
    /// </summary>
    public const string ProviderPlugins = "OTEL_DOTNET_AUTO_PLUGINS";

    /// <summary>
    /// Configuration key for controlling whether automatic instrumentation should set up OpenTelemetry .NET SDK at startup.
    /// </summary>
    public const string SetupSdk = "OTEL_DOTNET_AUTO_SETUP_SDK";

    /// <summary>
    /// Configuration key for enabling all instrumentations.
    /// </summary>
    public const string InstrumentationEnabled = "OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED";

    /// <summary>
    /// Configuration key for enabling all resource detectors.
    /// </summary>
    public const string ResourceDetectorEnabled = "OTEL_DOTNET_AUTO_RESOURCE_DETECTOR_ENABLED";

    /// <summary>
    /// Configuration key template for enabling resource detectors.
    /// </summary>
    public const string EnabledResourceDetectorTemplate = "OTEL_DOTNET_AUTO_{0}_RESOURCE_DETECTOR_ENABLED";

    /// <summary>
    /// Configuration keys for traces.
    /// </summary>
    public static class Traces
    {
        /// <summary>
        /// Configuration key for whether the tracer should be initialized by the profiler or not.
        /// </summary>
        public const string TracesEnabled = "OTEL_DOTNET_AUTO_TRACES_ENABLED";

        /// <summary>
        /// Configuration key for whether the OpenTracing tracer should be enabled.
        /// </summary>
        public const string OpenTracingEnabled = "OTEL_DOTNET_AUTO_OPENTRACING_ENABLED";

        /// <summary>
        /// Configuration key for the traces exporter to be used.
        /// Default is <c>"otlp"</c>.
        /// </summary>
        public const string Exporter = "OTEL_TRACES_EXPORTER";

        /// <summary>
        /// Configuration key for whether the trace console exporter is enabled.
        /// </summary>
        public const string ConsoleExporterEnabled = "OTEL_DOTNET_AUTO_TRACES_CONSOLE_EXPORTER_ENABLED";

        /// <summary>
        /// Configuration key for disabling all trace instrumentations.
        /// </summary>
        public const string TracesInstrumentationEnabled = "OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED";

        /// <summary>
        /// Configuration key template for disabled trace instrumentations.
        /// </summary>
        public const string EnabledTracesInstrumentationTemplate = "OTEL_DOTNET_AUTO_TRACES_{0}_INSTRUMENTATION_ENABLED";

        /// <summary>
        /// Configuration key for additional <see cref="ActivitySource"/> names to be added to the tracer at the startup.
        /// </summary>
        public const string AdditionalSources = "OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES";

        /// <summary>
        /// Configuration key for additional legacy source names to be added to the tracer at the startup.
        /// </summary>
        public const string AdditionalLegacySources = "OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_LEGACY_SOURCES";

        /// <summary>
        /// Configuration key for sampler to be used for traces.
        /// </summary>
        public const string TracesSampler = "OTEL_TRACES_SAMPLER";

        /// <summary>
        /// Configuration key for string value to be used as the sampler argument.
        /// </summary>
        public const string TracesSamplerArguments = "OTEL_TRACES_SAMPLER_ARG";

        /// <summary>
        /// Configuration keys for instrumentation options.
        /// </summary>
        public static class InstrumentationOptions
        {
            /// <summary>
            /// Configuration key for GraphQL instrumentation to enable passing query as a document attribute.
            /// </summary>
            public const string GraphQLSetDocument = "OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT";

            /// <summary>
            /// Configuration key for SQL Client instrumentation to enable passing text query as a db.statement attribute.
            /// </summary>
            public const string SqlClientSetDbStatementForText = "OTEL_DOTNET_AUTO_SQLCLIENT_SET_DBSTATEMENT_FOR_TEXT";

#if NET6_0_OR_GREATER
            /// <summary>
            /// Configuration key for Entity Framework Core instrumentation to enable passing text query as a db.statement attribute.
            /// </summary>
            public const string EntityFrameworkCoreSetDbStatementForText = "OTEL_DOTNET_AUTO_ENTITYFRAMEWORKCORE_SET_DBSTATEMENT_FOR_TEXT";
#endif
        }
    }

    /// <summary>
    /// Configuration keys for metrics.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Configuration key for whether the meter should be initialized by the profiler or not.
        /// </summary>
        public const string MetricsEnabled = "OTEL_DOTNET_AUTO_METRICS_ENABLED";

        /// <summary>
        /// Configuration key for the metrics exporter to be used.
        /// Default is <c>"otlp"</c>.
        /// </summary>
        public const string Exporter = "OTEL_METRICS_EXPORTER";

        /// <summary>
        /// Configuration key for whether the metrics console exporter is enabled.
        /// </summary>
        public const string ConsoleExporterEnabled = "OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED";

        /// <summary>
        /// Configuration key for disabling all metrics instrumentations.
        /// </summary>
        public const string MetricsInstrumentationEnabled = "OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED";

        /// <summary>
        /// Configuration key template for enabled metric instrumentations.
        /// </summary>
        public const string EnabledMetricsInstrumentationTemplate = "OTEL_DOTNET_AUTO_METRICS_{0}_INSTRUMENTATION_ENABLED";

        /// <summary>
        /// Configuration key for additional <see cref="Meter"/> names to be added to the meter at the startup.
        /// </summary>
        public const string AdditionalSources = "OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES";
    }

    /// <summary>
    /// Configuration keys for logs.
    /// </summary>
    public static class Logs
    {
        /// <summary>
        /// Configuration key for whether the logger should be initialized by the profiler or not.
        /// </summary>
        public const string LogsEnabled = "OTEL_DOTNET_AUTO_LOGS_ENABLED";

        /// <summary>
        /// Configuration key for the logs exporter to be used.
        /// Default is <c>"otlp"</c>.
        /// </summary>
        public const string Exporter = "OTEL_LOGS_EXPORTER";

        /// <summary>
        /// Configuration key for whether the logs console exporter is enabled.
        /// </summary>
        public const string ConsoleExporterEnabled = "OTEL_DOTNET_AUTO_LOGS_CONSOLE_EXPORTER_ENABLED";

        /// <summary>
        /// Configuration key for whether or not formatted log message
        /// should be included on generated <see cref="LogRecord"/>s.
        /// </summary>
        public const string IncludeFormattedMessage = "OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE";

        /// <summary>
        /// Configuration key for disabling all log instrumentations.
        /// </summary>
        public const string LogsInstrumentationEnabled = "OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED";

        /// <summary>
        /// Configuration key template for enabled log instrumentations.
        /// </summary>
        public const string EnabledLogsInstrumentationTemplate = "OTEL_DOTNET_AUTO_LOGS_{0}_INSTRUMENTATION_ENABLED";
    }

    /// <summary>
    /// Configuration keys for Sdk
    /// </summary>
    public static class Sdk
    {
        /// <summary>
        /// Configuration key for comma separated list of propagators to be used.
        /// Default is <c>"tracecontext,baggage"</c>.
        /// </summary>
        public const string Propagators = "OTEL_PROPAGATORS";
    }
}
