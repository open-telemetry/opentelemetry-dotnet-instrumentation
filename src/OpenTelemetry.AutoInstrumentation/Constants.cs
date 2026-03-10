// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation;

internal static class Constants
{
    public static class DistributionAttributes
    {
        public const string TelemetryDistroNameAttributeName = "telemetry.distro.name";
        public const string TelemetryDistroNameAttributeValue = "opentelemetry-dotnet-instrumentation";
        public const string TelemetryDistroVersionAttributeName = "telemetry.distro.version";
    }

    public static class GrpcSpanAttributes
    {
        public const string AttributeGrpcRequestMetadataPrefix = "rpc.grpc.request.metadata";
        public const string AttributeGrpcResponseMetadataPrefix = "rpc.grpc.response.metadata";
    }

    public static class HttpSpanAttributes
    {
        public const string AttributeHttpRequestHeaderPrefix = "http.request.header";
        public const string AttributeHttpResponseHeaderPrefix = "http.response.header";
    }

    public static class ResourceAttributes
    {
        public const string AttributeServiceName = "service.name";
        public const string AttributeServiceInstanceId = "service.instance.id";
    }

    public static class ConfigurationValues
    {
        public const string None = "none";

        /// <summary>
        /// Default delimiter for textual representation of multi-valued settings.
        /// </summary>
        public const char Separator = ',';

        /// <summary>a
        /// Delimiter for textual representation of settings that may contain multiple
        /// fully qualified .NET names, e.g.: assembly or type names, that already
        /// use commas as separators.
        /// </summary>
        public const char DotNetQualifiedNameSeparator = ':';

        public static class Exporters
        {
            public const string Otlp = "otlp";
            public const string Prometheus = "prometheus";
            public const string Zipkin = "zipkin";
            public const string Console = "console";
        }

        public static class Propagators
        {
            public const string W3CTraceContext = "tracecontext";
            public const string W3CBaggage = "baggage";
            public const string B3Multi = "b3multi";
            public const string B3Single = "b3";
        }

        public static class LogLevel
        {
            public const string Error = "error";
            public const string Warning = "warn";
            public const string Information = "info";
            public const string Debug = "debug";
        }

        public static class Loggers
        {
            public const string File = "file";
            public const string Console = "console";
        }
    }

    public static class EnvironmentVariables
    {
        public const string ProfilerEnabledVariable = "CORECLR_ENABLE_PROFILING";
        public const string ProfilerIdVariable = "CORECLR_PROFILER";
        public const string ProfilerPathVariable = "CORECLR_PROFILER_PATH";
        public const string Profiler32BitPathVariable = "CORECLR_PROFILER_PATH_32";
        public const string Profiler64BitPathVariable = "CORECLR_PROFILER_PATH_64";
        public const string ProfilerId = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";
    }
}
