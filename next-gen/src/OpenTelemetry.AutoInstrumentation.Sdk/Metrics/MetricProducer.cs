// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Base class for implementing a metric producer.
/// </summary>
public abstract class MetricProducer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricProducer"/> class.
    /// </summary>
    protected MetricProducer()
    {
    }

    /// <summary>
    /// Write metrics.
    /// </summary>
    /// <param name="writer"><see cref="MetricWriter"/>.</param>
    /// <returns><see langword="true"/> if the operation succeeded.</returns>
    public abstract bool WriteTo(MetricWriter writer);
}
