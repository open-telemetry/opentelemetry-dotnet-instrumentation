// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class DotNetLogs
{
    /// <summary>
    /// Gets or sets the ILogger logs instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "ilogger")]
    public object? ILogger { get; set; }

    /// <summary>
    /// Gets or sets the Log4Net logs instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "log4net")]
    public Log4NetBridgeEnabled? Log4Net { get; set; }

    /// <summary>
    /// Returns the list of enabled log instrumentations.
    /// </summary>
    public IReadOnlyList<LogInstrumentation> GetEnabledInstrumentations()
    {
        var result = new List<LogInstrumentation>();

        if (ILogger != null)
        {
            result.Add(LogInstrumentation.ILogger);
        }

        if (Log4Net != null)
        {
            result.Add(LogInstrumentation.Log4Net);
        }

        return result;
    }
}
