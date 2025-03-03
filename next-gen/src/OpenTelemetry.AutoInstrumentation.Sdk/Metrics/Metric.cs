// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about a metric.
/// </summary>
public sealed class Metric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Metric"/> class.
    /// </summary>
    /// <param name="metricType"><see cref="Metrics.MetricType"/>.</param>
    /// <param name="name">Name.</param>
    /// <param name="aggregationTemporality"><see cref="Metrics.AggregationTemporality"/>.</param>
    public Metric(
        MetricType metricType,
        string name,
        AggregationTemporality aggregationTemporality)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        MetricType = metricType;
        Name = name;
        AggregationTemporality = aggregationTemporality;

        if (IsSumNonMonotonic
            && AggregationTemporality == AggregationTemporality.Delta)
        {
            throw new NotSupportedException("Delta aggregation temporality cannot be used with non-monotonic sums");
        }
    }

    /// <summary>
    /// Gets the metric type.
    /// </summary>
    public MetricType MetricType { get; }

    /// <summary>
    /// Gets the metric name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the metric description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the metric unit.
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Gets the metric aggregation temporality.
    /// </summary>
    public AggregationTemporality AggregationTemporality { get; }

    /// <summary>
    /// Gets a value indicating whether or not the metric is sum non-monotonic.
    /// </summary>
    public bool IsSumNonMonotonic => ((byte)MetricType & 0x80) == 1;

    /// <summary>
    /// Gets a value indicating whether or not the metric is a floating point value.
    /// </summary>
    public bool IsFloatingPoint => ((byte)MetricType & 0x0c) == 1;
}
