// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Tracer Settings
/// </summary>
internal class TracerSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets a value indicating whether the tracer should be loaded by the profiler. Default is true.
    /// </summary>
    public bool TracesEnabled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the OpenTracing tracer is enabled. Default is false.
    /// </summary>
    public bool OpenTracingEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled traces exporters.
    /// </summary>
    public IReadOnlyList<TracesExporter> TracesExporters { get; private set; } = new List<TracesExporter>();

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IReadOnlyList<TracerInstrumentation> EnabledInstrumentations { get; private set; } = new List<TracerInstrumentation>();

    /// <summary>
    /// Gets the list of activity configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.AutoInstrumentation.*" };

    /// <summary>
    /// Gets the list of legacy configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> AdditionalLegacySources { get; } = new List<string>();

    /// <summary>
    /// Gets the instrumentation options.
    /// </summary>
    public InstrumentationOptions InstrumentationOptions { get; private set; } = new(new Configuration(failFast: false));

    /// <summary>
    /// Gets or sets a value indicating whether the console exporter is enabled.
    /// </summary>
    private bool ConsoleExporterEnabled { get; set; }

    protected override void OnLoad(Configuration configuration)
    {
        ConsoleExporterEnabled = configuration.GetBool(ConfigurationKeys.Traces.ConsoleExporterEnabled) ?? false;
        TracesExporters = ParseTracesExporter(configuration);

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Traces.TracesInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<TracerInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Traces.EnabledTracesInstrumentationTemplate);

        var additionalSources = configuration.GetString(ConfigurationKeys.Traces.AdditionalSources);
        if (additionalSources != null)
        {
            foreach (var configurationName in additionalSources.Split(Constants.ConfigurationValues.Separator))
            {
                ActivitySources.Add(configurationName);
            }
        }

        var additionalLegacySources = configuration.GetString(ConfigurationKeys.Traces.AdditionalLegacySources);
        if (additionalLegacySources != null)
        {
            foreach (var configurationName in additionalLegacySources.Split(Constants.ConfigurationValues.Separator))
            {
                AdditionalLegacySources.Add(configurationName);
            }
        }

        TracesEnabled = configuration.GetBool(ConfigurationKeys.Traces.TracesEnabled) ?? true;
        OpenTracingEnabled = configuration.GetBool(ConfigurationKeys.Traces.OpenTracingEnabled) ?? false;

        InstrumentationOptions = new InstrumentationOptions(configuration);
    }

    private IReadOnlyList<TracesExporter> ParseTracesExporter(Configuration configuration)
    {
        var tracesExporterEnvVar = configuration.GetString(ConfigurationKeys.Traces.Exporter);

        if (string.IsNullOrEmpty(tracesExporterEnvVar))
        {
            return new List<TracesExporter> { TracesExporter.Otlp };
        }

        var exporters = new List<TracesExporter>();

        var exporterNames = tracesExporterEnvVar!.Split(Constants.ConfigurationValues.Separator);

        foreach (var exporterName in exporterNames)
        {
            switch (exporterName)
            {
                case Constants.ConfigurationValues.Exporters.Otlp:
                    exporters.Add(TracesExporter.Otlp);
                    break;
                case Constants.ConfigurationValues.Exporters.Zipkin:
                    exporters.Add(TracesExporter.Zipkin);
                    break;
                case Constants.ConfigurationValues.None:
                    break;
                case Constants.ConfigurationValues.Exporters.Console:
                    exporters.Add(TracesExporter.Console);
                    break;
                default:
                    if (configuration.FailFast)
                    {
                        var message = $"Traces exporter '{exporterName}' is not supported.";
                        Logger.Error(message);
                        throw new NotSupportedException(message);
                    }

                    Logger.Error($"Traces exporter '{exporterName}' is not supported.");
                    break;
            }
        }

        if (ConsoleExporterEnabled)
        {
            Logger.Warning($"The '{ConfigurationKeys.Traces.ConsoleExporterEnabled}' environment variable is deprecated and " +
                "will be removed in the next minor release. " +
                "Please set the console exporter using OTEL_TRACES_EXPORTER environmental variable. " +
                "Refer to the updated documentation for details.");

            exporters.Add(TracesExporter.Console);
        }

        return exporters;
    }
}
