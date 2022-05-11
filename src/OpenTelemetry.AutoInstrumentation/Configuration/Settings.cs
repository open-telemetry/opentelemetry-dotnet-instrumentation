// <copyright file="Settings.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Exporter;

namespace OpenTelemetry.AutoInstrumentation.Configuration;
// TODO Move settings to more suitable place?

/// <summary>
/// Settings
/// </summary>
public class Settings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    private Settings(IConfigurationSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        TracesExporter = ParseTracesExporter(source);
        OtlpExportProtocol = GetExporterOtlpProtocol(source);

        ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.Traces.ConsoleExporterEnabled) ?? false;

        var instrumentations = new Dictionary<string, Instrumentation>();
        var enabledInstrumentations = source.GetString(ConfigurationKeys.Traces.Instrumentations);
        if (enabledInstrumentations != null)
        {
            foreach (var instrumentation in enabledInstrumentations.Split(separator: ','))
            {
                if (Enum.TryParse(instrumentation, out Instrumentation parsedType))
                {
                    instrumentations[instrumentation] = parsedType;
                }
                else
                {
                    throw new ArgumentException($"The \"{instrumentation}\" is not recognized as supported instrumentation and cannot be disabled");
                }
            }
        }

        var disabledInstrumentations = source.GetString(ConfigurationKeys.Traces.DisabledInstrumentations);
        if (disabledInstrumentations != null)
        {
            foreach (var instrumentation in disabledInstrumentations.Split(separator: ','))
            {
                instrumentations.Remove(instrumentation);
            }
        }

        EnabledInstrumentations = instrumentations.Values.ToList();

        var providerPlugins = source.GetString(ConfigurationKeys.Traces.ProviderPlugins);
        if (providerPlugins != null)
        {
            foreach (var pluginAssemblyQualifiedName in providerPlugins.Split(':'))
            {
                TracerPlugins.Add(pluginAssemblyQualifiedName);
            }
        }

        var additionalSources = source.GetString(ConfigurationKeys.Traces.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var sourceName in additionalSources.Split(separator: ','))
            {
                ActivitySources.Add(sourceName);
            }
        }

        var legacySources = source.GetString(ConfigurationKeys.Traces.LegacySources);
        if (legacySources != null)
        {
            foreach (var sourceName in legacySources.Split(separator: ','))
            {
                LegacySources.Add(sourceName);
            }
        }

        TraceEnabled = source.GetBool(ConfigurationKeys.Traces.Enabled) ?? true;
        LoadTracerAtStartup = source.GetBool(ConfigurationKeys.Traces.LoadTracerAtStartup) ?? true;

        Integrations = new IntegrationSettingsCollection(source);

        Http2UnencryptedSupportEnabled = source.GetBool(ConfigurationKeys.Http2UnencryptedSupportEnabled) ?? false;

        FlushOnUnhandledException = source.GetBool(ConfigurationKeys.FlushOnUnhandledException) ?? false;
    }

    /// <summary>
    /// Gets a value indicating whether tracing is enabled.
    /// Default is <c>true</c>.
    /// </summary>
    /// <seealso cref="ConfigurationKeys.Traces.Enabled"/>
    public bool TraceEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the tracer should be loaded by the profiler. Default is true.
    /// </summary>
    public bool LoadTracerAtStartup { get; }

    /// <summary>
    /// Gets the traces exporter.
    /// </summary>
    public TracesExporter TracesExporter { get; }

    /// <summary>
    /// Gets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
    /// </summary>
    public OtlpExportProtocol? OtlpExportProtocol { get; }

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; }

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IList<Instrumentation> EnabledInstrumentations { get; }

    /// <summary>
    /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
    /// </summary>
    public IList<string> TracerPlugins { get; } = new List<string>();

    /// <summary>
    /// Gets the list of activity sources to be added to the tracer at the startup.
    /// </summary>
    public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.AutoInstrumentation.*" };

    /// <summary>
    /// Gets the list of legacy sources to be added to the tracer at the startup.
    /// </summary>
    public IList<string> LegacySources { get; } = new List<string>();

    /// <summary>
    /// Gets a collection of <see cref="Integrations"/> keyed by integration name.
    /// </summary>
    public IntegrationSettingsCollection Integrations { get; }

    /// <summary>
    /// Gets a value indicating whether the `System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport`
    /// should be enabled.
    /// It is required by OTLP gRPC exporter on .NET Core 3.x.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Http2UnencryptedSupportEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="AppDomain.UnhandledException"/> event should trigger
    /// the flushing of telemetry data.
    /// Default is <c>false</c>.
    /// </summary>
    public bool FlushOnUnhandledException { get; }

    internal static Settings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
        };

        return new Settings(configurationSource);
    }

    private static OtlpExportProtocol? GetExporterOtlpProtocol(IConfigurationSource source)
    {
        // the default in SDK is grpc. http/protobuf should be default for our purposes
        var exporterOtlpProtocol = source.GetString(ConfigurationKeys.ExporterOtlpProtocol);

        if (string.IsNullOrEmpty(exporterOtlpProtocol))
        {
            // override settings only for http/protobuf
            return OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        }

        // null value here means that it will be handled by OTEL .NET SDK
        return null;
    }

    private static TracesExporter ParseTracesExporter(IConfigurationSource source)
    {
        var tracesExporterEnvVar = source.GetString(ConfigurationKeys.Traces.Exporter) ?? "otlp";
        switch (tracesExporterEnvVar)
        {
            case null:
            case "":
            case "otlp":
                return TracesExporter.Otlp;
            case "zipkin":
                return TracesExporter.Zipkin;
            case "jaeger":
                return TracesExporter.Jaeger;
            case "none":
                return TracesExporter.None;
            default:
                throw new FormatException($"Traces exporter '{tracesExporterEnvVar}' is not supported");
        }
    }
}
