// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

using static OpenTelemetry.OpenTelemetryProtocol.Metrics.OtlpMetricExporterAsync;

using OtlpCollector = OpenTelemetry.Proto.Collector.Metrics.V1;
using OtlpMetric = OpenTelemetry.Proto.Metrics.V1;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests;

internal sealed class OtlpMeasurementCollector : IDisposable
{
    private readonly MeterListener _MeterListener;
    private readonly Resource _Resource;
    private readonly HashSet<string> _AllowedMeterNames = new();
    private readonly List<CollectedMeasurement> _Measurements = new();
    private readonly object _Lock = new();
    private TaskCompletionSource<CollectedMeasurement>? _SingleMeasurementTcs;
    private TaskCompletionSource<List<CollectedMeasurement>>? _MultipleMeasurementsTcs;
    private int _ExpectedMeasurementCount;

    public OtlpMeasurementCollector(Resource resource)
    {
        _Resource = resource;
        _MeterListener = new MeterListener();
        _MeterListener.InstrumentPublished = (instrument, listener) =>
        {
            // Only enable measurements for explicitly allowed meters to avoid capturing runtime metrics
            if (_AllowedMeterNames.Contains(instrument.Meter.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        // Register callbacks for different measurement types
        _MeterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        _MeterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _MeterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _MeterListener.SetMeasurementEventCallback<float>(OnMeasurementRecorded);
        _MeterListener.SetMeasurementEventCallback<decimal>(OnMeasurementRecorded);
        _MeterListener.Start();
    }

    public void AllowMeter(string meterName)
    {
        lock (_Lock)
        {
            _AllowedMeterNames.Add(meterName);
        }
    }

    public void Dispose()
    {
        _MeterListener?.Dispose();
    }

    private void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
        where T : struct
    {
        var collectedMeasurement = ProcessMeasurement(instrument, measurement, tags);

        lock (_Lock)
        {
            _Measurements.Add(collectedMeasurement);

            // Signal single measurement completion
            _SingleMeasurementTcs?.TrySetResult(collectedMeasurement);

            // Signal multiple measurements completion if we've reached the expected count
            if (_MultipleMeasurementsTcs != null && _Measurements.Count >= _ExpectedMeasurementCount)
            {
                _MultipleMeasurementsTcs.TrySetResult(new List<CollectedMeasurement>(_Measurements));
            }
        }
    }

    private CollectedMeasurement ProcessMeasurement<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
        where T : struct
    {
        var writer = new OtlpMetricWriter();
        writer.BeginBatch(_Resource);

        writer.BeginInstrumentationScope(
            new(instrument.Meter.Name)
            {
                Version = instrument.Meter.Version,
                Attributes = instrument.Meter.Tags?.ToList() ?? new List<KeyValuePair<string, object?>>()
            });

        var metric = new Metric(
            name: instrument.Name,
            metricType: MetricType.LongSum,
            aggregationTemporality: AggregationTemporality.Delta)
        {
            Description = instrument.Description,
        };

        writer.BeginMetric(metric);
        WriteNumberMetricPoint(writer, DateTime.UtcNow, DateTime.UtcNow, measurement);
        writer.EndMetric();
        writer.EndInstrumentationScope();
        writer.EndBatch();

        using var stream = new MemoryStream(writer.Request.Buffer, 0, writer.Request.WritePosition);
        var metricsData = OtlpMetric.MetricsData.Parser.ParseFrom(stream);

        var request = new OtlpCollector.ExportMetricsServiceRequest();
        request.ResourceMetrics.Add(metricsData.ResourceMetrics);

        return new CollectedMeasurement
        {
            InstrumentName = instrument.Name,
            Value = measurement,
            Tags = tags.ToArray(),
            Request = request,
            Timestamp = DateTime.UtcNow
        };
    }

    private static void WriteNumberMetricPoint<T>(
        MetricWriter writer,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        T value)
        where T : struct
    {
        // Handle different numeric types
        var numberMetricPoint = value switch
        {
            int intValue => new NumberMetricPoint(startTimeUtc, endTimeUtc, intValue),
            long longValue => new NumberMetricPoint(startTimeUtc, endTimeUtc, longValue),
            double doubleValue => new NumberMetricPoint(startTimeUtc, endTimeUtc, doubleValue),
            float floatValue => new NumberMetricPoint(startTimeUtc, endTimeUtc, floatValue),
            decimal decimalValue => new NumberMetricPoint(startTimeUtc, endTimeUtc, (double)decimalValue),
            _ => throw new ArgumentException($"Unsupported measurement type: {typeof(T)}")
        };

        writer.WriteNumberMetricPoint(
            in numberMetricPoint,
            default,
            exemplars: default);
    }

    internal async Task<CollectedMeasurement> WaitForMeasurementAsync(TimeSpan timeout)
    {
        lock (_Lock)
        {
            if (_Measurements.Count > 0)
            {
                return _Measurements[_Measurements.Count - 1];
            }

            _SingleMeasurementTcs = new TaskCompletionSource<CollectedMeasurement>();
        }

        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(_SingleMeasurementTcs.Task, timeoutTask).ConfigureAwait(true);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("Measurement collection timed out");
        }

        return await _SingleMeasurementTcs.Task.ConfigureAwait(true);
    }

    internal async Task<List<CollectedMeasurement>> WaitForMeasurementsAsync(int count, TimeSpan timeout)
    {
        lock (_Lock)
        {
            if (_Measurements.Count >= count)
            {
                return _Measurements.Take(count).ToList();
            }

            _ExpectedMeasurementCount = count;
            _MultipleMeasurementsTcs = new TaskCompletionSource<List<CollectedMeasurement>>();
        }

        var timeoutTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(_MultipleMeasurementsTcs.Task, timeoutTask).ConfigureAwait(true);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"Collection of {count} measurements timed out");
        }

        return await _MultipleMeasurementsTcs.Task.ConfigureAwait(true);
    }
}
