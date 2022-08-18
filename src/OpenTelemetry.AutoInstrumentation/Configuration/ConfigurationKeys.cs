// <copyright file="ConfigurationKeys.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Configuration keys
/// </summary>
public class ConfigurationKeys
{
    /// <summary>
    /// Configuration key for the OTLP protocol to be used.
    /// Default is <c>"http/protobuf"</c>.
    /// </summary>
    public const string ExporterOtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>
    /// Configuration key for enabling `System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport`.
    /// It is required by OTLP gRPC exporter on .NET Core 3.x.
    /// Default is <c>false</c>.
    /// </summary>
    public const string Http2UnencryptedSupportEnabled = "OTEL_DOTNET_AUTO_HTTP2UNENCRYPTEDSUPPORT_ENABLED";

    /// <summary>
    /// Configuration key for setting the directory for the profiler's log files.
    /// If not set, default is
    /// "%ProgramData%"\OpenTelemetry .NET AutoInstrumentation\logs\" on Windows or
    /// "/var/log/opentelemetry/dotnet/" on Linux.
    /// </summary>
    public const string LogDirectory = "OTEL_DOTNET_AUTO_LOG_DIRECTORY";

    /// <summary>
    /// Configuration key for enabling the flushing of telemetry data when an unhandled exception occurs.
    /// </summary>
    public const string FlushOnUnhandledException = "OTEL_DOTNET_AUTO_FLUSH_ON_UNHANDLEDEXCEPTION";

    /// <summary>
    /// Configuration keys for trace exporter
    /// </summary>
    public static class Traces
    {
        /// <summary>
        /// Configuration key for enabling or disabling the Tracer.
        /// Default is value is true (enabled).
        /// </summary>
        /// <seealso cref="TracerSettings.TraceEnabled"/>
        public const string Enabled = "OTEL_DOTNET_AUTO_TRACES_ENABLED";

        /// <summary>
        /// Configuration key for whether the tracer should be initialized by the profiler or not.
        /// </summary>
        public const string LoadTracerAtStartup = "OTEL_DOTNET_AUTO_LOAD_TRACER_AT_STARTUP";

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
        /// Configuration key for comma separated list of enabled trace instrumentations.
        /// </summary>
        public const string Instrumentations = "OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for comma separated list of disabled trace instrumentations.
        /// </summary>
        public const string DisabledInstrumentations = "OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for colon (:) separated list of trace plugins represented by <see cref="System.Type.AssemblyQualifiedName"/>.
        /// </summary>
        public const string ProviderPlugins = "OTEL_DOTNET_AUTO_TRACES_PLUGINS";

        /// <summary>
        /// Configuration key for additional <see cref="ActivitySource"/> names to be added to the tracer at the startup.
        /// </summary>
        public const string AdditionalSources = "OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES";

        /// <summary>
        /// Configuration key for legacy source names to be added to the tracer at the startup.
        /// </summary>
        public const string LegacySources = "OTEL_DOTNET_AUTO_LEGACY_SOURCES";
    }

    /// <summary>
    /// Configuration keys for metrics exporter
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Configuration key for enabling or disabling the Meter.
        /// Default is value is true (enabled).
        /// </summary>
        public const string Enabled = "OTEL_DOTNET_AUTO_METRICS_ENABLED";

        /// <summary>
        /// Configuration key for whether the meter should be initialized by the profiler or not.
        /// </summary>
        public const string LoadMeterAtStartup = "OTEL_DOTNET_AUTO_LOAD_METER_AT_STARTUP";

        /// <summary>
        /// Configuration key for the metrics exporter to be used.
        /// Default is <c>"otlp"</c>.
        /// </summary>
        public const string Exporter = "OTEL_METRICS_EXPORTER";

        /// <summary>
        /// Configuration key for the time interval (in milliseconds) between the start of two metrics export attempts.
        /// </summary>
        public const string ExportInterval = "OTEL_METRIC_EXPORT_INTERVAL";

        /// <summary>
        /// Configuration key for whether the metrics console exporter is enabled.
        /// </summary>
        public const string ConsoleExporterEnabled = "OTEL_DOTNET_AUTO_METRICS_CONSOLE_EXPORTER_ENABLED";

        /// <summary>
        /// Configuration key for comma separated list of enabled metric instrumentations.
        /// </summary>
        public const string Instrumentations = "OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for comma separated list of disabled metric instrumentations.
        /// </summary>
        public const string DisabledInstrumentations = "OTEL_DOTNET_AUTO_METRICS_DISABLED_INSTRUMENTATIONS";

        /// <summary>
        /// Configuration key for colon (:) separated list of metric plugins represented by <see cref="System.Type.AssemblyQualifiedName"/>.
        /// </summary>
        public const string ProviderPlugins = "OTEL_DOTNET_AUTO_METRICS_PLUGINS";

        /// <summary>
        /// Configuration key for additional <see cref="Meter"/> names to be added to the meter at the startup.
        /// </summary>
        public const string AdditionalSources = "OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES";
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
