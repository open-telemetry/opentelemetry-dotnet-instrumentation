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

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Configuration keys
/// </summary>
public class ConfigurationKeys
{
    /// <summary>
    /// Configuration key for enabling or disabling the Tracer.
    /// Default is value is true (enabled).
    /// </summary>
    /// <seealso cref="Settings.TraceEnabled"/>
    public const string TraceEnabled = "OTEL_DOTNET_AUTO_TRACE_ENABLED";

    /// <summary>
    /// Configuration key for whether the tracer should be initialized by the profiler or not.
    /// </summary>
    public const string LoadTracerAtStartup = "OTEL_DOTNET_AUTO_LOAD_TRACER_AT_STARTUP";

    /// <summary>
    /// Configuration key for the traces exporter to be used.
    /// Default is <c>"otlp"</c>.
    /// </summary>
    public const string TracesExporter = "OTEL_TRACES_EXPORTER";

    /// <summary>
    /// Configuration key for the OTLP protocol to be used.
    /// Default is <c>"http/protobuf"</c>.
    /// </summary>
    public const string ExporterOtlpProtocol = "OTEL_EXPORTER_OTLP_PROTOCOL";

    /// <summary>
    /// Configuration key for whether the trace console exporter is enabled.
    /// </summary>
    public const string ConsoleTraceExporterEnabled = "OTEL_DOTNET_AUTO_TRACE_CONSOLE_EXPORTER_ENABLED";

    /// <summary>
    /// Configuration key for comma separated list of enabled trace instrumentations.
    /// </summary>
    public const string EnabledTraceInstrumentations = "OTEL_DOTNET_AUTO_TRACE_ENABLED_INSTRUMENTATIONS";

    /// <summary>
    /// Configuration key for comma separated list of disabled trace instrumentations.
    /// </summary>
    public const string DisabledTraceInstrumentations = "OTEL_DOTNET_AUTO_TRACE_DISABLED_INSTRUMENTATIONS";

    /// <summary>
    /// Configuration key for colon (:) separated list of trace plugins represented by <see cref="System.Type.AssemblyQualifiedName"/>.
    /// </summary>
    public const string TracerProviderPlugins = "OTEL_DOTNET_AUTO_TRACE_INSTRUMENTATION_PLUGINS";

    /// <summary>
    /// Configuration key for additional <see cref="ActivitySource"/> names to be added to the tracer at the startup.
    /// </summary>
    public const string AdditionalTraceSources = "OTEL_DOTNET_AUTO_TRACE_ADDITIONAL_SOURCES";

    /// <summary>
    /// Configuration key for legacy source names to be added to the tracer at the startup.
    /// </summary>
    public const string LegacySources = "OTEL_DOTNET_AUTO_LEGACY_SOURCES";

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
    /// String format patterns used to match integration-specific configuration keys.
    /// </summary>
    internal static class Integrations
    {
        /// <summary>
        /// Configuration key pattern for enabling or disabling an integration.
        /// </summary>
        public const string Enabled = "OTEL_DOTNET_AUTO_{0}_ENABLED";
    }
}
