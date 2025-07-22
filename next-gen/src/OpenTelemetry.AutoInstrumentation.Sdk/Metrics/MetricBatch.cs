// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

internal readonly ref struct MetricBatch : IBatch<MetricBatchWriter>
{
    private readonly ILogger _Logger;
    private readonly Resource _Resource;
    private readonly MetricProducer[] _Producers;

    public MetricBatch(
        ILogger logger,
        Resource resource,
        MetricProducer[] producers)
    {
        Debug.Assert(logger != null);
        Debug.Assert(resource != null);
        Debug.Assert(producers != null);

        _Logger = logger;
        _Resource = resource;
        _Producers = producers;
    }

    public bool WriteTo(
        MetricBatchWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.BeginBatch(_Resource);

        foreach (MetricProducer producer in _Producers)
        {
            try
            {
                bool result = producer.WriteTo(writer);

                _Logger.MetricsCollectionCompleted(result, producer.GetType().FullName);
            }
            catch (Exception ex)
            {
                _Logger.MetricsCollectionException(ex, producer.GetType().FullName);
            }
        }

        writer.EndBatch();

        return true;
    }
}
