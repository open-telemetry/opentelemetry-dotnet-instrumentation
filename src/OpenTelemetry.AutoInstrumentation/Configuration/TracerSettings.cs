// <copyright file="TracerSettings.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Configuration;
// TODO Move settings to more suitable place?

/// <summary>
/// Tracer Settings
/// </summary>
internal class TracerSettings : Settings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TracerSettings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    private TracerSettings(IConfigurationSource source)
        : base(source)
    {
        TracesExporter = ParseTracesExporter(source);
        ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.Traces.ConsoleExporterEnabled) ?? false;

        EnabledInstrumentations = source.ParseEnabledEnumList<TracerInstrumentation>(
            enabledConfiguration: ConfigurationKeys.Traces.Instrumentations,
            disabledConfiguration: ConfigurationKeys.Traces.DisabledInstrumentations,
            error: "The \"{0}\" is not recognized as supported trace instrumentation and cannot be enabled or disabled.");

        var additionalSources = source.GetString(ConfigurationKeys.Traces.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var sourceName in additionalSources.Split(Constants.ConfigurationValues.Separator))
            {
                ActivitySources.Add(sourceName);
            }
        }

        var legacySources = source.GetString(ConfigurationKeys.Traces.LegacySources);
        if (legacySources != null)
        {
            foreach (var sourceName in legacySources.Split(Constants.ConfigurationValues.Separator))
            {
                LegacySources.Add(sourceName);
            }
        }

        TracesEnabled = source.GetBool(ConfigurationKeys.Traces.TracesEnabled) ?? true;

        InstrumentationOptions = new InstrumentationOptions(source);
    }

    /// <summary>
    /// Gets a value indicating whether the tracer should be loaded by the profiler. Default is true.
    /// </summary>
    public bool TracesEnabled { get; }

    /// <summary>
    /// Gets the traces exporter.
    /// </summary>
    public TracesExporter TracesExporter { get; }

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; }

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IList<TracerInstrumentation> EnabledInstrumentations { get; }

    /// <summary>
    /// Gets the list of activity sources to be added to the tracer at the startup.
    /// </summary>
    public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.AutoInstrumentation.*" };

    /// <summary>
    /// Gets the list of legacy sources to be added to the tracer at the startup.
    /// </summary>
    public IList<string> LegacySources { get; } = new List<string>();

    /// <summary>
    /// Gets the instrumentation options.
    /// </summary>
    public InstrumentationOptions InstrumentationOptions { get; private set; }

    internal static TracerSettings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
        };

        return new TracerSettings(configurationSource);
    }

    private static TracesExporter ParseTracesExporter(IConfigurationSource source)
    {
        var tracesExporterEnvVar = source.GetString(ConfigurationKeys.Traces.Exporter)
            ?? Constants.ConfigurationValues.Exporters.Otlp;

        switch (tracesExporterEnvVar)
        {
            case null:
            case "":
            case Constants.ConfigurationValues.Exporters.Otlp:
                return TracesExporter.Otlp;
            case Constants.ConfigurationValues.Exporters.Zipkin:
                return TracesExporter.Zipkin;
            case Constants.ConfigurationValues.Exporters.Jaeger:
                return TracesExporter.Jaeger;
            case Constants.ConfigurationValues.None:
                return TracesExporter.None;
            default:
                throw new FormatException($"Traces exporter '{tracesExporterEnvVar}' is not supported");
        }
    }
}
