// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Describes the contract for exporting batches of metrics asynchronously.
/// </summary>
public interface IMetricExporterAsync : IExporterAsync<MetricBatchWriter>
{
    /// <summary>
    /// Gets a value indicating whether or not the exporter supports delta aggregation temporality.
    /// </summary>
    bool SupportsDeltaAggregationTemporality { get; }
}
