// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about a histogram metric point bucket.
/// </summary>
public readonly record struct HistogramMetricPointBucket
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistogramMetricPointBucket"/> struct.
    /// </summary>
    /// <param name="value">Bucket value.</param>
    /// <param name="count">Bucket count.</param>
    public HistogramMetricPointBucket(
        double value,
        int count)
    {
        Value = value;
        Count = count;
    }

    /// <summary>
    /// Gets the histogram metric point bucket value.
    /// </summary>
    public double Value { get; }

    /// <summary>
    /// Gets the histogram metric point bucket count.
    /// </summary>
    public int Count { get; }
}
