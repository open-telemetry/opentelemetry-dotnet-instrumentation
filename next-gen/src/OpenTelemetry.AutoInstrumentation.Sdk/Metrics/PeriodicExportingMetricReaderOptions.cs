// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores periodic exporting metric reader options.
/// </summary>
public sealed class PeriodicExportingMetricReaderOptions : MetricReaderOptions
{
    private const int DefaultExportIntervalMilliseconds = 5000;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeriodicExportingMetricReaderOptions"/> class.
    /// </summary>
    /// <param name="exportIntervalMilliseconds">Export interval in milliseconds. Default value: 5000.</param>
    /// <param name="exportTimeoutMilliseconds">Export timeout in milliseconds. Default value: 30000.</param>
    public PeriodicExportingMetricReaderOptions(
        int exportIntervalMilliseconds = DefaultExportIntervalMilliseconds,
        int exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds)
    : base(exportTimeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exportIntervalMilliseconds, 1);

        ExportIntervalMilliseconds = exportIntervalMilliseconds;
    }

    /// <summary>
    /// Gets the delay interval (in milliseconds) between two consecutive exports. The default value is 5000.
    /// </summary>
    public int ExportIntervalMilliseconds { get; }
}
