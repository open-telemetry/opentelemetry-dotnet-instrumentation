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

using System;
using System.Collections.Generic;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Log Settings
/// </summary>
internal class LogSettings : Settings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogSettings"/> class
    /// using the specified <see cref="IConfigurationSource"/> to initialize values.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    public LogSettings(IConfigurationSource source)
        : base(source)
    {
        LogExporter = ParseLogExporter(source);
        ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.Logs.ConsoleExporterEnabled) ?? false;
        IncludeFormattedMessage = source.GetBool(ConfigurationKeys.Logs.IncludeFormattedMessage) ?? false;

        EnabledInstrumentations = source.ParseEnabledEnumList<LogInstrumentation>(
            enabledConfiguration: ConfigurationKeys.Logs.Instrumentations,
            disabledConfiguration: ConfigurationKeys.Logs.DisabledInstrumentations,
            error: "The \"{0}\" is not recognized as supported logs instrumentation and cannot be enabled or disabled.");
    }

    /// <summary>
    /// Gets the logs exporter.
    /// </summary>
    public LogExporter LogExporter { get; }

    /// <summary>
    /// Gets a value indicating whether the IncludeFormattedMessage is enabled.
    /// </summary>
    public bool IncludeFormattedMessage { get; }

    /// <summary>
    /// Gets a value indicating whether the console exporter is enabled.
    /// </summary>
    public bool ConsoleExporterEnabled { get; }

    /// <summary>
    /// Gets the list of enabled instrumentations.
    /// </summary>
    public IList<LogInstrumentation> EnabledInstrumentations { get; }

    private static LogExporter ParseLogExporter(IConfigurationSource source)
    {
        var logExporterEnvVar = source.GetString(ConfigurationKeys.Logs.Exporter)
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
                throw new FormatException($"Log exporter '{logExporterEnvVar}' is not supported");
        }
    }
}
