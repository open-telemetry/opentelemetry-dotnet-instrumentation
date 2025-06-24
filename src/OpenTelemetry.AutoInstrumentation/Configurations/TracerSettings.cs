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
    /// Gets tracing OTLP Settings.
    /// </summary>
    public OtlpSettings? OtlpSettings { get; private set; }

    /// <summary>
    /// Gets tracing Zipkin Settings.
    /// </summary>
    public ZipkinExporterConfig? ZipkinSettings { get; private set; }

    /// <summary>
    /// Gets tracing Batch Processor Configuration.
    /// </summary>
    public BatchProcessorConfig BatchProcessorConfig { get; private set; } = new();

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        BatchProcessorConfig = new BatchProcessorConfig(
            scheduleDelay: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.ScheduleDelay),
            exportTimeout: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.ExportTimeout),
            maxQueueSize: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.MaxQueueSize),
            maxExportBatchSize: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.MaxExportBatchSize));

        TracesExporters = ParseTracesExporter(configuration);
        if (TracesExporters.Contains(TracesExporter.Otlp))
        {
            OtlpSettings = new OtlpSettings(OtlpSignalType.Traces, configuration);
        }

        if (TracesExporters.Contains(TracesExporter.Zipkin))
        {
            ZipkinSettings = new ZipkinExporterConfig(configuration.GetString(ConfigurationKeys.Traces.ZipkinEndpoint));
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

    protected override void OnLoadFile(Conf configuration)
    {
        if (configuration.TracerProvider != null &&
            configuration.TracerProvider.Processors != null &&
            configuration.TracerProvider.Processors.TryGetValue("batch", out var batchProcessorConfig))
        {
            TracesEnabled = true;
            BatchProcessorConfig = batchProcessorConfig;
            var exporters = BatchProcessorConfig.Exporter;
            var tracesExporters = new List<TracesExporter>();
            if (exporters != null)
            {
                if (exporters.OtlpGrpc != null)
                {
                    tracesExporters.Add(TracesExporter.Otlp);
                    OtlpSettings = new OtlpSettings(OtlpSignalType.Traces, exporters.OtlpGrpc);
                }

                if (exporters.OtlpHttp != null)
                {
                    tracesExporters.Add(TracesExporter.Otlp);
                    OtlpSettings = new OtlpSettings(OtlpSignalType.Traces, exporters.OtlpHttp);
                }

                if (exporters.Zipkin != null)
                {
                    tracesExporters.Add(TracesExporter.Zipkin);
                    ZipkinSettings = exporters.Zipkin;
                }

                if (exporters.Console != null)
                {
                    tracesExporters.Add(TracesExporter.Console);
                }

                TracesExporters = tracesExporters;
            }
        }
        else
        {
            TracesEnabled = false;
        }

        var tracesInstrumentations = configuration.InstrumentationDevelopment?.DotNet?.Traces;
        if (tracesInstrumentations != null)
        {
            EnabledInstrumentations = tracesInstrumentations.GetEnabledInstrumentations();
        }

        InstrumentationOptions = new InstrumentationOptions(tracesInstrumentations);
    }

    private static IReadOnlyList<TracesExporter> ParseTracesExporter(Configuration configuration)
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
