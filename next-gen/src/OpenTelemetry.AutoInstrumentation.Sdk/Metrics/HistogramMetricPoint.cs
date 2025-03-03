// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about a histogram metric point.
/// </summary>
public readonly ref struct HistogramMetricPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistogramMetricPoint"/> struct.
    /// </summary>
    /// <param name="startTimeUtc">Start time.</param>
    /// <param name="endTimeUtc">End time.</param>
    /// <param name="features"><see cref="HistogramMetricPointFeatures"/>.</param>
    /// <param name="min">Min value.</param>
    /// <param name="max">Max value.</param>
    /// <param name="sum">Sum value.</param>
    /// <param name="count">Count value.</param>
    public HistogramMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        HistogramMetricPointFeatures features,
        double min,
        double max,
        double sum,
        long count)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        Features = features;
        Min = min;
        Max = max;
        Sum = sum;
        Count = count;
    }

    /// <summary>
    /// Gets the histogram metric point start time.
    /// </summary>
    public DateTime StartTimeUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the histogram metric point end time.
    /// </summary>
    public DateTime EndTimeUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the histogram metric point features.
    /// </summary>
    public HistogramMetricPointFeatures Features { get; }

    /// <summary>
    /// Gets the histogram metric point min value.
    /// </summary>
    public double Min { get; }

    /// <summary>
    /// Gets the histogram metric point max value.
    /// </summary>
    public double Max { get; }

    /// <summary>
    /// Gets the histogram metric point sum value.
    /// </summary>
    public double Sum { get; }

    /// <summary>
    /// Gets the histogram metric point count value.
    /// </summary>
    public long Count { get; }
}
