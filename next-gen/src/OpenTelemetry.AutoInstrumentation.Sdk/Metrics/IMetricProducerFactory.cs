// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Describes the contract for creating metrics producer instances.
/// </summary>
public interface IMetricProducerFactory
{
    /// <summary>
    /// Create a metric producer.
    /// </summary>
    /// <param name="options"><see cref="MetricProducerOptions"/>.</param>
    /// <returns><see cref="MetricProducer"/>.</returns>
    MetricProducer Create(MetricProducerOptions options);
}
