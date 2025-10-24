// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
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
    /// For File based configuration, this must be empty,
    /// and the configuration will be handled by Processors.
    /// </summary>
    public IReadOnlyList<TracesExporter> TracesExporters { get; private set; } = [];

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IReadOnlyList<TracerInstrumentation> EnabledInstrumentations { get; private set; } = [];

    /// <summary>
    /// Gets the list of activity configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> ActivitySources { get; } = ["OpenTelemetry.AutoInstrumentation.*"];

    /// <summary>
    /// Gets the list of legacy configurations to be added to the tracer at the startup.
    /// </summary>
    public IList<string> AdditionalLegacySources { get; } = [];

    /// <summary>
    /// Gets the instrumentation options.
    /// </summary>
    public InstrumentationOptions InstrumentationOptions { get; private set; } = new(new Configuration(failFast: false));

    /// <summary>
    /// Gets tracing OTLP Settings.
    /// </summary>
    public OtlpSettings? OtlpSettings { get; private set; }

    /// <summary>
    /// Gets tracing OTLP Settings.
    /// For environment variable configuration, this must be null,
    /// and the configuration will be handled by TracesExporters
    /// </summary>
    public IReadOnlyList<ProcessorConfig>? Processors { get; private set; } = null;

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        TracesExporters = ParseTracesExporter(configuration);
        if (TracesExporters.Contains(TracesExporter.Otlp))
        {
            OtlpSettings = new OtlpSettings(OtlpSignalType.Traces, configuration);
        }

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

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        var processors = configuration.TracerProvider?.Processors;
        if (processors != null && processors.Count > 0)
        {
            TracesEnabled = true;
        }

        Processors = processors;

        EnabledInstrumentations = configuration.InstrumentationDevelopment?.DotNet?.Traces?.GetEnabledInstrumentations() ?? [];

        InstrumentationOptions = new InstrumentationOptions(configuration.InstrumentationDevelopment?.DotNet?.Traces);
    }

    private static List<TracesExporter> ParseTracesExporter(Configuration configuration)
    {
        var tracesExporterEnvVar = configuration.GetString(ConfigurationKeys.Traces.Exporter);
        var exporters = new List<TracesExporter>();
        var seenExporters = new HashSet<string>();

        if (string.IsNullOrEmpty(tracesExporterEnvVar))
        {
            exporters.Add(TracesExporter.Otlp);
            return exporters;
        }

        var exporterNames = tracesExporterEnvVar!.Split(Constants.ConfigurationValues.Separator);

        foreach (var exporterName in exporterNames)
        {
            if (seenExporters.Contains(exporterName))
            {
                var message = $"Duplicate traces exporter '{exporterName}' found.";
                if (configuration.FailFast)
                {
                    Logger.Error(message);
                    throw new NotSupportedException(message);
                }

                Logger.Warning(message);
                continue;
            }

            seenExporters.Add(exporterName);

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
                    var unsupportedMessage = $"Traces exporter '{exporterName}' is not supported.";
                    Logger.Error(unsupportedMessage);

                    if (configuration.FailFast)
                    {
                        throw new NotSupportedException(unsupportedMessage);
                    }

                    break;
            }
        }

        return exporters;
    }
}
