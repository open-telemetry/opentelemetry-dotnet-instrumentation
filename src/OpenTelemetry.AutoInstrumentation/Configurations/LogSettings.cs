// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IReadOnlyList<LogInstrumentation> EnabledInstrumentations { get; private set; } = new List<LogInstrumentation>();

    /// <summary>
    /// Gets or sets a value indicating whether the console exporter is enabled.
    /// </summary>
    private bool ConsoleExporterEnabled { get; set; }

    protected override void OnLoad(Configuration configuration)
    {
        LogsEnabled = configuration.GetBool(ConfigurationKeys.Logs.LogsEnabled) ?? true;
        ConsoleExporterEnabled = configuration.GetBool(ConfigurationKeys.Logs.ConsoleExporterEnabled) ?? false;
        LogExporters = ParseLogExporter(configuration);
        IncludeFormattedMessage = configuration.GetBool(ConfigurationKeys.Logs.IncludeFormattedMessage) ?? false;

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Logs.LogsInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<LogInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate);
    }

    private IReadOnlyList<LogExporter> ParseLogExporter(Configuration configuration)
    {
        var logExporterEnvVar = configuration.GetString(ConfigurationKeys.Logs.Exporter);

        if (string.IsNullOrEmpty(logExporterEnvVar))
        {
            return new List<LogExporter> { LogExporter.Otlp };
        }

        var exporters = new List<LogExporter>();
        var exporterNames = logExporterEnvVar!.Split(Constants.ConfigurationValues.Separator);

        foreach (var exporterName in exporterNames)
        {
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
                    if (configuration.FailFast)
                    {
                        var message = $"Log exporter '{exporterName}' is not supported.";
                        Logger.Error(message);
                        throw new NotSupportedException(message);
                    }

                    Logger.Error($"Log exporter '{exporterName}' is not supported.");
                    break;
            }
        }

        if (ConsoleExporterEnabled)
        {
            Logger.Warning($"The '{ConfigurationKeys.Logs.ConsoleExporterEnabled}' environment variable is deprecated and " +
                "will be removed in the next minor release. " +
                "Please update your configuration to use the new method. " +
                "Refer to the updated documentation for details.");

            exporters.Add(LogExporter.Console);
        }

        return exporters;
    }
}
