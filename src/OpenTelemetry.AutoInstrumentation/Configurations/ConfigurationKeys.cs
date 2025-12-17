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
    /// Configuration key template for resource attributes.
    /// </summary>
    public const string ResourceAttributes = "OTEL_RESOURCE_ATTRIBUTES";

    /// <summary>
    /// Configuration key for setting the service name.
    /// </summary>
    public const string ServiceName = "OTEL_SERVICE_NAME";

    /// <summary>
    /// Configuration keys for file based configuration.
    /// </summary>
    public static class FileBasedConfiguration
    {
        /// <summary>
        /// Configuration key for enabling file based configuration.
        /// </summary>
        public const string Enabled = "OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED";

        /// <summary>
        /// Configuration key for the path to the configuration file.
        /// Default is <c>"config.yaml"</c>.
        /// </summary>
        public const string FileName = "OTEL_EXPERIMENTAL_CONFIG_FILE";
    }

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
        /// Configuration key for enabling IL rewriting of SqlClient on .NET Framework to ensure CommandText is available.
        /// </summary>
        public const string SqlClientNetFxILRewriteEnabled = "OTEL_DOTNET_AUTO_SQLCLIENT_NETFX_ILREWRITE_ENABLED";

        /// <summary>
        /// Configuration keys for instrumentation options.
        /// </summary>
        public static class InstrumentationOptions
        {
#if NETFRAMEWORK
            /// <summary>
            /// Configuration key for ASP.NET instrumentation to enable capturing HTTP request headers as span tags.
            /// </summary>
            public const string AspNetInstrumentationCaptureRequestHeaders = "OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS";

            /// <summary>
            /// Configuration key for ASP.NET instrumentation to enable capturing HTTP response headers as span tags.
            /// </summary>
            public const string AspNetInstrumentationCaptureResponseHeaders = "OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS";
#endif

#if NET
            /// <summary>
            /// Configuration key for ASP.NET Core instrumentation to enable capturing HTTP request headers as span tags.
            /// </summary>
            public const string AspNetCoreInstrumentationCaptureRequestHeaders = "OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS";

            /// <summary>
            /// Configuration key for ASP.NET Core instrumentation to enable capturing HTTP response headers as span tags.
            /// </summary>
            public const string AspNetCoreInstrumentationCaptureResponseHeaders = "OTEL_DOTNET_AUTO_TRACES_ASPNETCORE_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS";

            /// <summary>
            /// Configuration key for GraphQL instrumentation to enable passing query as a document attribute.
            /// </summary>
            public const string GraphQLSetDocument = "OTEL_DOTNET_AUTO_GRAPHQL_SET_DOCUMENT";
#endif

            /// <summary>
            /// Configuration key for GrpcNetClient instrumentation to enable capturing request metadata as span tags.
            /// </summary>
            public const string GrpcNetClientInstrumentationCaptureRequestMetadata = "OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_REQUEST_METADATA";

            /// <summary>
            /// Configuration key for GrpcNetClient instrumentation to enable capturing response metadata as span tags.
            /// </summary>
            public const string GrpcNetClientInstrumentationCaptureResponseMetadata = "OTEL_DOTNET_AUTO_TRACES_GRPCNETCLIENT_INSTRUMENTATION_CAPTURE_RESPONSE_METADATA";

            /// <summary>
            /// Configuration key for HTTP instrumentation to enable capturing HTTP request headers as span tags.
            /// </summary>
            public const string HttpInstrumentationCaptureRequestHeaders = "OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS";

            /// <summary>
            /// Configuration key for HTTP instrumentation to enable capturing HTTP response headers as span tags.
            /// </summary>
            public const string HttpInstrumentationCaptureResponseHeaders = "OTEL_DOTNET_AUTO_TRACES_HTTP_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS";

            /// <summary>
            /// Configuration key for Oracle Client instrumentation to enable passing text query as a db.statement attribute.
            /// </summary>
            public const string OracleMdaSetDbStatementForText = "OTEL_DOTNET_AUTO_ORACLEMDA_SET_DBSTATEMENT_FOR_TEXT";
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
        /// Configuration key for whether or not formatted log message
        /// should be included on generated <see cref="LogRecord"/>s.
        /// </summary>
        public const string IncludeFormattedMessage = "OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE";

        /// <summary>
        /// Configuration key for whether or not experimental log4net bridge
        /// should be enabled.
        /// </summary>
        public const string EnableLog4NetBridge = "OTEL_DOTNET_AUTO_LOGS_ENABLE_LOG4NET_BRIDGE";

        /// <summary>
        /// Configuration key for whether or not experimental NLog bridge
        /// should be enabled.
        /// </summary>
        public const string EnableNLogBridge = "OTEL_DOTNET_AUTO_LOGS_ENABLE_NLOG_BRIDGE";

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
