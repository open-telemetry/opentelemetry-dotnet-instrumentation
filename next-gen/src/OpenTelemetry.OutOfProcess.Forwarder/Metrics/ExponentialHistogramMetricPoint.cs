// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about an exponential histogram metric point.
/// </summary>
public readonly ref struct ExponentialHistogramMetricPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialHistogramMetricPoint"/> struct.
    /// </summary>
    /// <param name="startTimeUtc">Start time.</param>
    /// <param name="endTimeUtc">End time.</param>
    /// <param name="features"><see cref="HistogramMetricPointFeatures"/>.</param>
    /// <param name="min">Min value.</param>
    /// <param name="max">Max value.</param>
    /// <param name="sum">Sum value.</param>
    /// <param name="count">Count value.</param>
    /// <param name="scale">Scale value.</param>
    /// <param name="zeroCount">Zero count value.</param>
    public ExponentialHistogramMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        HistogramMetricPointFeatures features,
        double min,
        double max,
        double sum,
        long count,
        int scale,
        long zeroCount)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        Features = features |= HistogramMetricPointFeatures.BucketsRecorded;
        Min = min;
        Max = max;
        Sum = sum;
        Count = count;
        Scale = scale;
        ZeroCount = zeroCount;
    }

    /// <summary>
    /// Gets the exponential histogram metric point start time.
    /// </summary>
    public DateTime StartTimeUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the exponential histogram metric point end time.
    /// </summary>
    public DateTime EndTimeUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the exponential histogram metric point features.
    /// </summary>
    public HistogramMetricPointFeatures Features { get; }

    /// <summary>
    /// Gets the exponential histogram metric point min value.
    /// </summary>
    public double Min { get; }

    /// <summary>
    /// Gets the exponential histogram metric point max value.
    /// </summary>
    public double Max { get; }

    /// <summary>
    /// Gets the exponential histogram metric point sum value.
    /// </summary>
    public double Sum { get; }

    /// <summary>
    /// Gets the exponential histogram metric point count value.
    /// </summary>
    public long Count { get; }

    /// <summary>
    /// Gets the exponential histogram metric point scale value.
    /// </summary>
    public int Scale { get; }

    /// <summary>
    /// Gets the exponential histogram metric point zero count value.
    /// </summary>
    public long ZeroCount { get; }
}
