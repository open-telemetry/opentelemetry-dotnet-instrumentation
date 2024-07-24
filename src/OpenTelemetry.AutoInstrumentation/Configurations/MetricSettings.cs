// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Metric Settings
/// </summary>
internal class MetricSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets a value indicating whether the metrics should be loaded by the profiler. Default is true.
    /// </summary>
    public bool MetricsEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled metrics exporters.
    /// </summary>
    public IReadOnlyList<MetricsExporter> MetricExporters { get; private set; } = new List<MetricsExporter>();

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled meters.
    /// </summary>
    public IReadOnlyList<MetricInstrumentation> EnabledInstrumentations { get; private set; } = new List<MetricInstrumentation>();

    /// <summary>
    /// Gets the list of meters to be added to the MeterProvider at the startup.
    /// </summary>
    public IList<string> Meters { get; } = new List<string>();

    protected override void OnLoad(Configuration configuration)
    {
        MetricExporters = ParseMetricExporter(configuration);
        ConsoleExporterEnabled = configuration.GetBool(ConfigurationKeys.Metrics.ConsoleExporterEnabled) ?? false;

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Metrics.MetricsInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<MetricInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Metrics.EnabledMetricsInstrumentationTemplate);

        var additionalSources = configuration.GetString(ConfigurationKeys.Metrics.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var sourceName in additionalSources.Split(Constants.ConfigurationValues.Separator))
            {
                Meters.Add(sourceName);
            }
        }

        MetricsEnabled = configuration.GetBool(ConfigurationKeys.Metrics.MetricsEnabled) ?? true;
    }

    private static IReadOnlyList<MetricsExporter> ParseMetricExporter(Configuration configuration)
    {
        var metricsExporterEnvVar = configuration.GetString(ConfigurationKeys.Metrics.Exporter);

        if (string.IsNullOrEmpty(metricsExporterEnvVar))
        {
            return new List<MetricsExporter> { MetricsExporter.Otlp };
        }

        var exporters = new List<MetricsExporter>();
        var exporterNames = metricsExporterEnvVar!.Split(Constants.ConfigurationValues.Separator);

        foreach (var exporterName in exporterNames)
        {
            switch (exporterName)
            {
                case Constants.ConfigurationValues.Exporters.Otlp:
                    exporters.Add(MetricsExporter.Otlp);
                    break;
                case Constants.ConfigurationValues.Exporters.Prometheus:
                    exporters.Add(MetricsExporter.Prometheus);
                    break;
                case Constants.ConfigurationValues.None:
                    break;

                default:
                    if (configuration.FailFast)
                    {
                        var message = $"Metric exporter '{exporterName}' is not supported.";
                        Logger.Error(message);
                        throw new NotSupportedException(message);
                    }

                    Logger.Error($"Metric exporter '{exporterName}' is not supported.");
                    break;
            }
        }

        return exporters;
    }
}
