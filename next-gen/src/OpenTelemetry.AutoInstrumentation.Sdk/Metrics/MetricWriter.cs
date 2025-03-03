// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Describes the contract for writing metrics.
/// </summary>
public abstract class MetricWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricWriter"/> class.
    /// </summary>
    protected MetricWriter()
    {
    }

    /// <inheritdoc cref="IBatchWriter.BeginInstrumentationScope(InstrumentationScope)"/>
    public virtual void BeginInstrumentationScope(
        InstrumentationScope instrumentationScope)
    {
    }

    /// <inheritdoc cref="IBatchWriter.EndInstrumentationScope"/>
    public virtual void EndInstrumentationScope()
    {
    }

    /// <summary>
    /// Begin a metric.
    /// </summary>
    /// <param name="metric"><see cref="Metric"/>.</param>
    public virtual void BeginMetric(
        Metric metric)
    {
    }

    /// <summary>
    /// End a metric.
    /// </summary>
    public virtual void EndMetric()
    {
    }

    /// <summary>
    /// Write a number metric point.
    /// </summary>
    /// <param name="numberMetricPoint"><see cref="NumberMetricPoint"/>.</param>
    /// <param name="attributes">Metric point attributes.</param>
    /// <param name="exemplars">Metric point exemplars.</param>
    public virtual void WriteNumberMetricPoint(
        in NumberMetricPoint numberMetricPoint,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes,
        ReadOnlySpan<Exemplar> exemplars)
    {
    }

    /// <summary>
    /// Write a histogram metric point.
    /// </summary>
    /// <param name="histogramMetricPoint"><see cref="HistogramMetricPoint"/>.</param>
    /// <param name="buckets"><see cref="HistogramMetricPointBucket"/>s.</param>
    /// <param name="attributes">Metric point attributes.</param>
    /// <param name="exemplars">Metric point exemplars.</param>
    public virtual void WriteHistogramMetricPoint(
        in HistogramMetricPoint histogramMetricPoint,
        ReadOnlySpan<HistogramMetricPointBucket> buckets,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes,
        ReadOnlySpan<Exemplar> exemplars)
    {
    }

    /// <summary>
    /// Write an exponential histogram metric point.
    /// </summary>
    /// <param name="exponentialHistogramMetricPoint"><see cref="ExponentialHistogramMetricPoint"/>.</param>
    /// <param name="positiveBuckets">Positive <see cref="ExponentialHistogramMetricPointBuckets"/>.</param>
    /// <param name="negativeBuckets">Negative <see cref="ExponentialHistogramMetricPointBuckets"/>.</param>
    /// <param name="attributes">Metric point attributes.</param>
    /// <param name="exemplars">Metric point exemplars.</param>
    public virtual void WriteExponentialHistogramMetricPoint(
        in ExponentialHistogramMetricPoint exponentialHistogramMetricPoint,
        in ExponentialHistogramMetricPointBuckets positiveBuckets,
        in ExponentialHistogramMetricPointBuckets negativeBuckets,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes,
        ReadOnlySpan<Exemplar> exemplars)
    {
    }

    /// <summary>
    /// Write a summary metric point.
    /// </summary>
    /// <param name="summaryMetricPoint"><see cref="SummaryMetricPoint"/>.</param>
    /// <param name="quantiles"><see cref="SummaryMetricPointQuantile"/>s.</param>
    /// <param name="attributes">Metric point attributes.</param>
    public virtual void WriteSummaryMetricPoint(
        in SummaryMetricPoint summaryMetricPoint,
        ReadOnlySpan<SummaryMetricPointQuantile> quantiles,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
    }
}
