// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Implements a factory for creating metric readers.
/// </summary>
public static class MetricReaderFactory
{
    /// <summary>
    /// Create an asynchronous periodic exporting metric reader.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    /// <param name="resource"><see cref="Resource"/>.</param>
    /// <param name="exporter"><see cref="IMetricExporterAsync"/>.</param>
    /// <param name="metricProducerFactories">Option metric producer factories.</param>
    /// <param name="options"><see cref="PeriodicExportingMetricReaderOptions"/>.</param>
    /// <returns><see cref="IMetricReader"/>.</returns>
    public static IMetricReader CreatePeriodicExportingMetricReaderAsync(
        ILoggerFactory loggerFactory,
        Resource resource,
        IMetricExporterAsync exporter,
        IEnumerable<IMetricProducerFactory>? metricProducerFactories,
        PeriodicExportingMetricReaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(exporter);

        return new PeriodicExportingMetricReaderAsync(
            loggerFactory.CreateLogger<PeriodicExportingMetricReaderAsync>(),
            resource,
            exporter,
            metricProducerFactories,
            options);
    }
}
