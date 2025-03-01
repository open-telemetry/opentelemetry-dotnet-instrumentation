// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about a summary metric point.
/// </summary>
public readonly ref struct SummaryMetricPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryMetricPoint"/> struct.
    /// </summary>
    /// <param name="startTimeUtc">Start time.</param>
    /// <param name="endTimeUtc">End time.</param>
    /// <param name="sum">Sum value.</param>
    /// <param name="count">Count value.</param>
    public SummaryMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        double sum,
        int count)
    {
        StartTimeUtc = startTimeUtc;
        EndTimeUtc = endTimeUtc;
        Sum = sum;
        Count = count;
    }

    /// <summary>
    /// Gets the summary metric point start time.
    /// </summary>
    public DateTime StartTimeUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the summary metric point end time.
    /// </summary>
    public DateTime EndTimeUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the summary metric point sum value.
    /// </summary>
    public double Sum { get; }

    /// <summary>
    /// Gets the summary metric point count value.
    /// </summary>
    public int Count { get; }
}
