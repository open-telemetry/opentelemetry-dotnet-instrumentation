// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Contains metric producer options.
/// </summary>
public sealed class MetricProducerOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricProducerOptions"/> class.
    /// </summary>
    /// <param name="aggregationTemporality"><see cref="Metrics.AggregationTemporality"/>.</param>
    internal MetricProducerOptions(
        AggregationTemporality aggregationTemporality)
    {
        AggregationTemporality = aggregationTemporality;
    }

    /// <summary>
    /// Gets the <see cref="Metrics.AggregationTemporality"/>.
    /// </summary>
    public AggregationTemporality AggregationTemporality { get; }
}
