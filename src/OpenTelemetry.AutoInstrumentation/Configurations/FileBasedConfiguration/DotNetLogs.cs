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
    public LogBridgeEnabled? Log4Net { get; set; }

    /// <summary>
    /// Gets or sets the Log4Net logs instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "nlog")]
    public LogBridgeEnabled? NLog { get; set; }

    /// <summary>
    /// Gets or sets the Serilog logs instrumentation configuration.
    /// </summary>
    [YamlMember(Alias = "serilog")]
    public LogBridgeEnabled? Serilog { get; set; }

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

        if (NLog != null)
        {
            result.Add(LogInstrumentation.NLog);
        }

        if (Serilog != null)
        {
            result.Add(LogInstrumentation.Serilog);
        }

        return result;
    }
}
