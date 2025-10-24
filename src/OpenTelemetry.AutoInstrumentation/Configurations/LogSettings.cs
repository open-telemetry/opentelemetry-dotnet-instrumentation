// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
using OpenTelemetry.AutoInstrumentation.Logging;

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
    /// Gets the processors configured via file-based configuration.
    /// </summary>
    public IReadOnlyList<LogProcessorConfig>? Processors { get; private set; }

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        LogsEnabled = configuration.GetBool(ConfigurationKeys.Logs.LogsEnabled) ?? true;
        LogExporters = ParseLogExporter(configuration);
        if (LogExporters.Contains(LogExporter.Otlp))
        {
            OtlpSettings = new OtlpSettings(OtlpSignalType.Logs, configuration);
        }

        Processors = null;
        IncludeFormattedMessage = configuration.GetBool(ConfigurationKeys.Logs.IncludeFormattedMessage) ?? false;
        EnableLog4NetBridge = configuration.GetBool(ConfigurationKeys.Logs.EnableLog4NetBridge) ?? false;

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Logs.LogsInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<LogInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate);
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        var processors = configuration.LoggerProvider?.Processors;

        LogsEnabled = processors != null && processors.Count > 0;
        LogExporters = Array.Empty<LogExporter>();
        OtlpSettings = null;
        Processors = processors;
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
