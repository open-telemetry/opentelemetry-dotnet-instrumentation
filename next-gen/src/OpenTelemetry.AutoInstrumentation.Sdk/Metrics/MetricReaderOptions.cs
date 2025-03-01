// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores metric reader options.
/// </summary>
public class MetricReaderOptions
{
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricReaderOptions"/> class.
    /// </summary>
    /// <param name="exportTimeoutMilliseconds">Export timeout in milliseconds. Default value: 30000.</param>
    public MetricReaderOptions(
        int exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(exportTimeoutMilliseconds, -1);

        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }

    /// <summary>
    /// Gets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    public int ExportTimeoutMilliseconds { get; }

    /// <summary>
    /// Gets or sets the <see cref="AggregationTemporality" /> preference.
    /// </summary>
    public AggregationTemporality TemporalityPreference
    {
        get => field == AggregationTemporality.Unspecified ? AggregationTemporality.Cumulative : field;
        set => field = value;
    }
}
