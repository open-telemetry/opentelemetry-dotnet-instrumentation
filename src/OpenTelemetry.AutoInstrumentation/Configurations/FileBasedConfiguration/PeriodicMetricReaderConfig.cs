// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class PeriodicMetricReaderConfig
{
    /// <summary>
    /// Gets or sets the delay interval (in milliseconds) between start of two consecutive exports.
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
    /// Gets or sets the exporter configuration.
    /// </summary>
    [YamlMember(Alias = "exporter")]
    public MetricPeriodicExporterConfig? Exporter { get; set; }

    public void CopyTo(PeriodicExportingMetricReaderOptions options)
    {
        options.ExportIntervalMilliseconds = Interval;
        options.ExportTimeoutMilliseconds = Timeout;
    }
}
