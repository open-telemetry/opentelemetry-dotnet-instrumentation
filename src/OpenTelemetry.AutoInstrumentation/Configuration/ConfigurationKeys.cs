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
    public const string TraceEnabled = "OTEL_DOTNET_AUTO_ENABLED";

    /// <summary>
    /// Configuration key for whether the tracer should be initialized by the profiler or not.
    /// </summary>
    public const string LoadTracerAtStartup = "OTEL_DOTNET_AUTO_LOAD_AT_STARTUP";

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
    /// Configuration key for whether the console exporter is enabled.
    /// </summary>
    public const string ConsoleExporterEnabled = "OTEL_DOTNET_AUTO_CONSOLE_EXPORTER_ENABLED";

    /// <summary>
    /// Configuration key for comma separated list of enabled instrumentations.
    /// </summary>
    public const string Instrumentations = "OTEL_DOTNET_AUTO_ENABLED_INSTRUMENTATIONS";

    /// <summary>
    /// Configuration key for comma separated list of disabled instrumentations.
    /// </summary>
    public const string DisabledInstrumentations = "OTEL_DOTNET_AUTO_DISABLED_INSTRUMENTATIONS";

    /// <summary>
    /// Configuration key for colon (:) separated list of plugins represented by <see cref="System.Type.AssemblyQualifiedName"/>.
    /// </summary>
    public const string ProviderPlugins = "OTEL_DOTNET_AUTO_INSTRUMENTATION_PLUGINS";

    /// <summary>
    /// Configuration key for additional <see cref="ActivitySource"/> names to be added to the tracer at the startup.
    /// </summary>
    public const string AdditionalSources = "OTEL_DOTNET_AUTO_ADDITIONAL_SOURCES";

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
