// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Core.Tokens;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Log Settings
/// </summary>
internal class LogSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets a value indicating whether the logs should be loaded by the profiler. Default is true.
    /// </summary>
    public bool LogsEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled logs exporters.
    /// </summary>
    public IReadOnlyList<LogExporter> LogExporters { get; private set; } = new List<LogExporter>();

    /// <summary>
    /// Gets a value indicating whether the IncludeFormattedMessage is enabled.
    /// </summary>
    public bool IncludeFormattedMessage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the experimental log4net bridge is enabled.
    /// </summary>
    public bool EnableLog4NetBridge { get; private set; }

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IReadOnlyList<LogInstrumentation> EnabledInstrumentations { get; private set; } = new List<LogInstrumentation>();

    /// <summary>
    /// Gets logs OTLP Settings.
    /// </summary>
    public OtlpSettings? OtlpSettings { get; private set; }

    /// <summary>
    /// Gets tracing Batch Processor Configuration.
    /// </summary>
    public BatchProcessorConfig BatchProcessorConfig { get; private set; } = new();

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        LogsEnabled = configuration.GetBool(ConfigurationKeys.Logs.LogsEnabled) ?? true;
        LogExporters = ParseLogExporter(configuration);
        if (LogExporters.Contains(LogExporter.Otlp))
        {
            OtlpSettings = new OtlpSettings(OtlpSignalType.Logs, configuration);
        }

        BatchProcessorConfig = new BatchProcessorConfig(
            scheduleDelay: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.ScheduleDelay),
            exportTimeout: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.ExportTimeout),
            maxQueueSize: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.MaxQueueSize),
            maxExportBatchSize: configuration.GetInt32(ConfigurationKeys.Traces.BatchSpanProcessorConfig.MaxExportBatchSize));

        IncludeFormattedMessage = configuration.GetBool(ConfigurationKeys.Logs.IncludeFormattedMessage) ?? false;
        EnableLog4NetBridge = configuration.GetBool(ConfigurationKeys.Logs.EnableLog4NetBridge) ?? false;

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Logs.LogsInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<LogInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate);
    }

    protected override void OnLoadFile(Conf configuration)
    {
        if (configuration.LoggerProvider != null &&
            configuration.LoggerProvider.Processors != null &&
            configuration.LoggerProvider.Processors.TryGetValue("batch", out var batchProcessorConfig))
        {
            BatchProcessorConfig = batchProcessorConfig;
            LogsEnabled = true;
            var logExporters = new List<LogExporter>();
            var exporters = batchProcessorConfig.Exporter;
            if (exporters != null)
            {
                if (exporters.OtlpGrpc != null)
                {
                    logExporters.Add(LogExporter.Otlp);
                    OtlpSettings = new OtlpSettings(OtlpSignalType.Logs, exporters.OtlpGrpc);
                }

                if (exporters.OtlpHttp != null)
                {
                    logExporters.Add(LogExporter.Otlp);
                    OtlpSettings = new OtlpSettings(OtlpSignalType.Logs, exporters.OtlpHttp);
                }

                if (exporters.Console != null)
                {
                    logExporters.Add(LogExporter.Console);
                }

                LogExporters = logExporters;
            }
        }
        else
        {
            LogsEnabled = false;
        }

        var logsInstrumentations = configuration.InstrumentationDevelopment?.DotNet?.Logs;

        if (logsInstrumentations != null)
        {
            EnabledInstrumentations = logsInstrumentations.GetEnabledInstrumentations();
        }
    }

    private static IReadOnlyList<LogExporter> ParseLogExporter(Configuration configuration)
    {
        var logExporterEnvVar = configuration.GetString(ConfigurationKeys.Logs.Exporter);
        var exporters = new List<LogExporter>();
        var seenExporters = new HashSet<string>();

        if (string.IsNullOrEmpty(logExporterEnvVar))
        {
            exporters.Add(LogExporter.Otlp);
            return exporters;
        }

        var exporterNames = logExporterEnvVar!.Split(Constants.ConfigurationValues.Separator);

        foreach (var exporterName in exporterNames)
        {
            if (seenExporters.Contains(exporterName))
            {
                var message = $"Duplicate log exporter '{exporterName}' found.";
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
                    exporters.Add(LogExporter.Otlp);
                    break;
                case Constants.ConfigurationValues.Exporters.Console:
                    exporters.Add(LogExporter.Console);
                    break;
                case Constants.ConfigurationValues.None:
                    break;
                default:
                    var unsupportedMessage = $"Log exporter '{exporterName}' is not supported.";
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
