// <copyright file="MetricSettings.cs" company="OpenTelemetry Authors">
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

/// <summary>
/// Metric Settings
/// </summary>
public class MetricSettings : Settings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricSettings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    private MetricSettings(IConfigurationSource source)
        : base(source)
    {
        MetricExporter = ParseMetricExporter(source);
        ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.Metrics.ConsoleExporterEnabled) ?? false;

        EnabledInstrumentations = source.ParseEnabledEnumList<MetricInstrumentation>(
            enabledConfiguration: ConfigurationKeys.Metrics.Instrumentations,
            disabledConfiguration: ConfigurationKeys.Metrics.DisabledInstrumentations,
            error: "The \"{0}\" is not recognized as supported metrics instrumentation and cannot be enabled or disabled.");

        var additionalSources = source.GetString(ConfigurationKeys.Metrics.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var sourceName in additionalSources.Split(Constants.ConfigurationValues.Separator))
            {
                Meters.Add(sourceName);
            }
        }

        MetricExportInterval = source.GetInt32(ConfigurationKeys.Metrics.ExportInterval);
        LoadMetricsAtStartup = source.GetBool(ConfigurationKeys.Metrics.LoadMeterAtStartup) ?? true;
    }

    /// <summary>
    /// Gets a value indicating whether the metrics should be loaded by the profiler. Default is true.
    /// </summary>
    public bool LoadMetricsAtStartup { get; }

    /// <summary>
    /// Gets the metrics exporter.
    /// </summary>
    public MetricsExporter MetricExporter { get; }

    /// <summary>
    /// Gets the metrics export interval.
    /// </summary>
    public int? MetricExportInterval { get; }

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; }

    /// <summary>
    /// Gets the list of enabled meters.
    /// </summary>
    public IList<MetricInstrumentation> EnabledInstrumentations { get; }

    /// <summary>
    /// Gets the list of meters to be added to the MeterProvider at the startup.
    /// </summary>
    public IList<string> Meters { get; } = new List<string>();

    internal static MetricSettings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
        };

        return new MetricSettings(configurationSource);
    }

    private static MetricsExporter ParseMetricExporter(IConfigurationSource source)
    {
        var metricsExporterEnvVar = source.GetString(ConfigurationKeys.Metrics.Exporter)
                                    ?? Constants.ConfigurationValues.Exporters.Otlp;

        switch (metricsExporterEnvVar)
        {
            case null:
            case "":
            case Constants.ConfigurationValues.Exporters.Otlp:
                return MetricsExporter.Otlp;
            case Constants.ConfigurationValues.Exporters.Prometheus:
                return MetricsExporter.Prometheus;
            case Constants.ConfigurationValues.None:
                return MetricsExporter.None;
            default:
                throw new FormatException($"Metric exporter '{metricsExporterEnvVar}' is not supported");
        }
    }
}
