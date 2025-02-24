// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about exponential histogram metric point buckets.
/// </summary>
public readonly ref struct ExponentialHistogramMetricPointBuckets
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialHistogramMetricPointBuckets"/> struct.
    /// </summary>
    /// <param name="offset">Offset value.</param>
    /// <param name="counts">Counts.</param>
    public ExponentialHistogramMetricPointBuckets(
        int offset,
        ReadOnlySpan<long> counts)
    {
        Offset = offset;
        Counts = counts;
    }

    /// <summary>
    /// Gets the exponential histogram metric point buckets offset value.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the exponential histogram metric point buckets counts.
    /// </summary>
    public ReadOnlySpan<long> Counts { get; }
}
