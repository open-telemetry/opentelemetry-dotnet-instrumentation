// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ReaderConfiguration
{
    public ReaderConfiguration()
    {
    }

    public ReaderConfiguration(int? interval, int? timeout)
    {
        if (interval is not null)
        {
            Interval = interval.Value;
        }

        if (timeout is not null)
        {
            Timeout = timeout.Value;
        }
    }

    /// <summary>
    /// Gets or sets the delay interval (in milliseconds) between the start of two consecutive exports.
    /// Value must be non-negative.
    /// If omitted or null, 60000 is used.
    /// </summary>
    [YamlMember(Alias = "interval")]
    public int Interval { get; set; } = 60000;

    /// <summary>
    /// Gets or sets the maximum allowed time (in milliseconds) to export data.
    /// Value must be non-negative. A value of 0 indicates no limit (infinity).
    /// If omitted or null, 30000 is used.
    /// </summary>
    [YamlMember(Alias = "timeout")]
    public int Timeout { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the exporter configuration for the reader.
    /// </summary>
    [YamlMember(Alias = "exporter")]
    public ExporterConfig Exporter { get; set; } = new();

    public PeriodicExportingMetricReaderOptions ToPeriodicExportingMetricReaderOptions()
    {
        return new PeriodicExportingMetricReaderOptions
        {
            ExportIntervalMilliseconds = Interval,
            ExportTimeoutMilliseconds = Timeout
        };
    }
}
