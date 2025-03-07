// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

internal class MetricReaderAsync : IMetricReader
{
    private readonly ILogger _Logger;
    private readonly Resource _Resource;
    private readonly IMetricExporterAsync _Exporter;
    private readonly MetricProducer[] _MetricProducers;
    private readonly int _ExportTimeoutMilliseconds;
    private CancellationTokenSource? _ExportCts;
    private bool _Disposed;

    public MetricReaderAsync(
        ILogger<MetricReaderAsync> logger,
        Resource resource,
        IMetricExporterAsync exporter,
        IEnumerable<IMetricProducerFactory>? metricProducerFactories,
        MetricReaderOptions options)
        : this((ILogger)logger, resource, exporter, metricProducerFactories, options)
    {
    }

    protected MetricReaderAsync(
        ILogger logger,
        Resource resource,
        IMetricExporterAsync exporter,
        IEnumerable<IMetricProducerFactory>? metricProducerFactories,
        MetricReaderOptions options)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(options);

        _Logger = logger;
        _Resource = resource;
        _Exporter = exporter;

        var aggregationTemporality = options.AggregationTemporalityPreference == AggregationTemporality.Delta
            ? exporter.SupportsDeltaAggregationTemporality
                ? AggregationTemporality.Delta
                : AggregationTemporality.Cumulative
            : options.AggregationTemporalityPreference;

        Debug.Assert(aggregationTemporality != AggregationTemporality.Unspecified);

        var producerOptions = new MetricProducerOptions(aggregationTemporality);

        _MetricProducers = (metricProducerFactories ?? Array.Empty<IMetricProducerFactory>())
            .Where(f => f != null)
            .Select(f => f.Create(producerOptions) ?? throw new InvalidOperationException($"{nameof(IMetricProducerFactory)} '{f.GetType()}' returned a null instance"))
            .ToArray();

        _ExportTimeoutMilliseconds = options.ExportTimeoutMilliseconds;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public virtual Task CollectAsync(CancellationToken cancellationToken)
        => OnCollectAsync(cancellationToken);

    public virtual Task FlushAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public virtual Task ShutdownAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    protected async Task OnCollectAsync(CancellationToken cancellationToken)
    {
        CancellationToken exportCancellationToken;
        if (_ExportTimeoutMilliseconds < 0)
        {
            exportCancellationToken = CancellationToken.None;
        }
        else
        {
            if (_ExportCts == null || !_ExportCts.TryReset())
            {
                var oldCts = _ExportCts;
                _ExportCts = new CancellationTokenSource(_ExportTimeoutMilliseconds);
                oldCts?.Dispose();
            }

            exportCancellationToken = _ExportCts.Token;
        }

        bool suppliedTokenIsDefault = cancellationToken == default;

        try
        {
            bool result;

            if (suppliedTokenIsDefault)
            {
                result = await ExportAsync(exportCancellationToken).ConfigureAwait(false);
            }
            else if (exportCancellationToken != default)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, exportCancellationToken);

                result = await ExportAsync(cts.Token).ConfigureAwait(false);
            }
            else
            {
                result = await ExportAsync(cancellationToken).ConfigureAwait(false);
            }

            _Logger.TelemetryExportCompleted(result, _Exporter.GetType().FullName);
        }
        catch (Exception ex)
        {
            _Logger.TelemetryExportException(ex, _Exporter.GetType().FullName);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_Disposed)
        {
            if (disposing)
            {
                foreach (var metricProducer in _MetricProducers)
                {
                    if (metricProducer is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                _ExportCts?.Dispose();
                _Exporter.Dispose();
            }

            _Disposed = true;
        }
    }

    private Task<bool> ExportAsync(
        CancellationToken cancellationToken)
    {
        var batch = new MetricBatch(_Logger, _Resource, _MetricProducers);

        return _Exporter.ExportAsync(in batch, cancellationToken);
    }
}
