// <copyright file="LogSettings.cs" company="OpenTelemetry Authors">
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
    /// Gets the logs exporter.
    /// </summary>
    public LogExporter LogExporter { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the IncludeFormattedMessage is enabled.
    /// </summary>
    public bool IncludeFormattedMessage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; private set; }

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IReadOnlyList<LogInstrumentation> EnabledInstrumentations { get; private set; } = new List<LogInstrumentation>();

    protected override void OnLoad(Configuration configuration)
    {
        LogsEnabled = configuration.GetBool(ConfigurationKeys.Logs.LogsEnabled) ?? true;
        LogExporter = ParseLogExporter(configuration);
        ConsoleExporterEnabled = configuration.GetBool(ConfigurationKeys.Logs.ConsoleExporterEnabled) ?? false;
        IncludeFormattedMessage = configuration.GetBool(ConfigurationKeys.Logs.IncludeFormattedMessage) ?? false;

        var instrumentationEnabledByDefault =
            configuration.GetBool(ConfigurationKeys.Logs.LogsInstrumentationEnabled) ??
            configuration.GetBool(ConfigurationKeys.InstrumentationEnabled) ?? true;

        EnabledInstrumentations = configuration.ParseEnabledEnumList<LogInstrumentation>(
            enabledByDefault: instrumentationEnabledByDefault,
            enabledConfigurationTemplate: ConfigurationKeys.Logs.EnabledLogsInstrumentationTemplate);
    }

    private static LogExporter ParseLogExporter(Configuration configuration)
    {
        var logExporterEnvVar = configuration.GetString(ConfigurationKeys.Logs.Exporter)
            ?? Constants.ConfigurationValues.Exporters.Otlp;

        switch (logExporterEnvVar)
        {
            case null:
            case "":
            case Constants.ConfigurationValues.Exporters.Otlp:
                return LogExporter.Otlp;
            case Constants.ConfigurationValues.None:
                return LogExporter.None;
            default:
                Logger.Error($"Log exporter '{logExporterEnvVar}' is not supported. Defaulting to '{Constants.ConfigurationValues.Exporters.Otlp}'.");
                return LogExporter.Otlp;
        }
    }
}
