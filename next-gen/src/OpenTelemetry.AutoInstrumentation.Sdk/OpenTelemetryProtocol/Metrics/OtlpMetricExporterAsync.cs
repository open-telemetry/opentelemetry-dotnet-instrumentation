// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

using OtlpCollectorMetrics = OpenTelemetry.Proto.Collector.Metrics.V1;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpMetrics = OpenTelemetry.Proto.Metrics.V1;

namespace OpenTelemetry.OpenTelemetryProtocol.Metrics;

/// <summary>
/// OTLP metric exporter.
/// </summary>
internal sealed class OtlpMetricExporterAsync : OtlpExporterAsync<OtlpCollectorMetrics.ExportMetricsServiceRequest, MetricBatchWriter>, IMetricExporterAsync
{
    private readonly OtlpMetricWriter _Writer = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpMetricExporterAsync"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    public OtlpMetricExporterAsync(
        ILogger<OtlpMetricExporterAsync> logger,
        OtlpExporterOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public bool SupportsDeltaAggregationTemporality => true;

    /// <inheritdoc/>
    public override Task<bool> ExportAsync<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
    {
        var writer = _Writer;

        if (!batch.WriteTo(writer))
        {
            writer.Reset();
            return Task.FromResult(false);
        }

        return SendAsync(writer, cancellationToken);
    }

    private sealed class OtlpMetricWriter : MetricBatchWriter, IOtlpBatchWriter<OtlpCollectorMetrics.ExportMetricsServiceRequest>
    {
        private OtlpMetrics.ResourceMetrics? _OtlpResourceMetrics;
        private OtlpMetrics.ScopeMetrics? _OtlpScopeMetrics;
        private OtlpMetrics.Metric? _OtlpMetric;
        private Metric? _Metric;

        public OtlpMetricWriter()
        {
            Reset();
        }

        public OtlpCollectorMetrics.ExportMetricsServiceRequest Request { get; private set; }

        [MemberNotNull(nameof(Request))]
        public void Reset()
        {
            Request = new();
            _OtlpResourceMetrics = null;
            _OtlpScopeMetrics = null;
            _OtlpMetric = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(_OtlpResourceMetrics == null);

            _OtlpResourceMetrics = new();

            _OtlpResourceMetrics.Resource = new();

            foreach (var resourceAttribute in resource.Attributes)
            {
                _OtlpResourceMetrics.Resource.Attributes.Add(
                    new OtlpCommon.KeyValue
                    {
                        Key = resourceAttribute.Key,
                        Value = new()
                        {
                            StringValue = Convert.ToString(resourceAttribute.Value, CultureInfo.InvariantCulture) // todo: handle other types
                        }
                    });
            }
        }

        public override void EndBatch()
        {
            Debug.Assert(_OtlpResourceMetrics != null);

            Request.ResourceMetrics.Add(_OtlpResourceMetrics);
            _OtlpResourceMetrics = null;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);
            Debug.Assert(_OtlpResourceMetrics != null);
            Debug.Assert(_OtlpScopeMetrics == null);

            _OtlpScopeMetrics = _OtlpResourceMetrics.ScopeMetrics.FirstOrDefault(s => s.Scope.Name == instrumentationScope.Name);
            if (_OtlpScopeMetrics == null)
            {
                _OtlpScopeMetrics = new()
                {
                    Scope = new()
                    {
                        Name = instrumentationScope.Name,
                    }
                };

                if (!string.IsNullOrEmpty(instrumentationScope.Version))
                {
                    _OtlpScopeMetrics.Scope.Version = instrumentationScope.Version;
                }

                _OtlpResourceMetrics.ScopeMetrics.Add(_OtlpScopeMetrics);
            }
        }

        public override void EndInstrumentationScope()
        {
            Debug.Assert(_OtlpScopeMetrics != null);

            _OtlpScopeMetrics = null;
        }

        public override void BeginMetric(Metric metric)
        {
            Debug.Assert(metric != null);
            Debug.Assert(_OtlpResourceMetrics != null);
            Debug.Assert(_OtlpScopeMetrics != null);
            Debug.Assert(_OtlpMetric == null);

            _Metric = metric;

            _OtlpMetric = new OtlpMetrics.Metric
            {
                Name = metric.Name
            };

            if (metric.Description != null)
            {
                _OtlpMetric.Description = metric.Description;
            }

            if (metric.Unit != null)
            {
                _OtlpMetric.Unit = metric.Unit;
            }

            var aggregationTemporality = (OtlpMetrics.AggregationTemporality)(int)metric.AggregationTemporality;

            switch (metric.MetricType)
            {
                case MetricType.LongSum:
                case MetricType.LongSumNonMonotonic:
                case MetricType.DoubleSum:
                case MetricType.DoubleSumNonMonotonic:
                    _OtlpMetric.Sum = new()
                    {
                        IsMonotonic = !metric.IsSumNonMonotonic,
                        AggregationTemporality = aggregationTemporality,
                    };
                    break;
                case MetricType.LongGauge:
                case MetricType.DoubleGauge:
                    _OtlpMetric.Gauge = new();
                    break;
                case MetricType.Histogram:
                    _OtlpMetric.Histogram = new()
                    {
                        AggregationTemporality = aggregationTemporality
                    };
                    break;
                case MetricType.ExponentialHistogram:
                    _OtlpMetric.ExponentialHistogram = new()
                    {
                        AggregationTemporality = aggregationTemporality
                    };
                    break;
                case MetricType.Summary:
                    _OtlpMetric.Summary = new();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public override void EndMetric()
        {
            Debug.Assert(_OtlpScopeMetrics != null);
            Debug.Assert(_OtlpMetric != null);

            _OtlpScopeMetrics.Metrics.Add(_OtlpMetric);
            _OtlpMetric = null;
            _Metric = null;
        }

        public override void WriteNumberMetricPoint(
            in NumberMetricPoint numberMetricPoint,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ReadOnlySpan<Exemplar> exemplars)
        {
            Debug.Assert(_Metric != null);
            Debug.Assert(_OtlpMetric != null);

            var dataPoint = new OtlpMetrics.NumberDataPoint
            {
                StartTimeUnixNano = numberMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = numberMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            if (!_Metric.IsFloatingPoint)
            {
                dataPoint.AsInt = numberMetricPoint.ValueAsLong;
            }
            else
            {
                dataPoint.AsDouble = numberMetricPoint.ValueAsDouble;
            }

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            foreach (ref readonly var exemplar in exemplars)
            {
                OtlpMetrics.OtlpMetricsExtensions.AddExemplar(
                    dataPoint.Exemplars,
                    _Metric,
                    in exemplar);
            }

            if (_OtlpMetric.Sum != null)
            {
                _OtlpMetric.Sum.DataPoints.Add(dataPoint);
            }
            else if (_OtlpMetric.Gauge != null)
            {
                _OtlpMetric.Gauge.DataPoints.Add(dataPoint);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override void WriteHistogramMetricPoint(
            in HistogramMetricPoint histogramMetricPoint,
            ReadOnlySpan<HistogramMetricPointBucket> buckets,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ReadOnlySpan<Exemplar> exemplars)
        {
            Debug.Assert(_Metric != null);
            Debug.Assert(_OtlpMetric?.Histogram != null);

            var dataPoint = new OtlpMetrics.HistogramDataPoint
            {
                StartTimeUnixNano = histogramMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = histogramMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            dataPoint.Count = (ulong)histogramMetricPoint.Count;
            dataPoint.Sum = histogramMetricPoint.Sum;

            if (histogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded))
            {
                dataPoint.Min = histogramMetricPoint.Min;
                dataPoint.Max = histogramMetricPoint.Max;
            }

            if (histogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.BucketsRecorded))
            {
                foreach (ref readonly var bucket in buckets)
                {
                    dataPoint.BucketCounts.Add((ulong)bucket.Count);
                    if (bucket.Value != double.PositiveInfinity)
                    {
                        dataPoint.ExplicitBounds.Add(bucket.Value);
                    }
                }
            }

            foreach (ref readonly var exemplar in exemplars)
            {
                OtlpMetrics.OtlpMetricsExtensions.AddExemplar(
                    dataPoint.Exemplars,
                    _Metric,
                    in exemplar);
            }

            _OtlpMetric.Histogram.DataPoints.Add(dataPoint);
        }

        public override void WriteExponentialHistogramMetricPoint(
            in ExponentialHistogramMetricPoint exponentialHistogramMetricPoint,
            in ExponentialHistogramMetricPointBuckets positiveBuckets,
            in ExponentialHistogramMetricPointBuckets negativeBuckets,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ReadOnlySpan<Exemplar> exemplars)
        {
            Debug.Assert(_Metric != null);
            Debug.Assert(_OtlpMetric?.ExponentialHistogram != null);

            var dataPoint = new OtlpMetrics.ExponentialHistogramDataPoint
            {
                StartTimeUnixNano = exponentialHistogramMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = exponentialHistogramMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            dataPoint.Count = (ulong)exponentialHistogramMetricPoint.Count;
            dataPoint.Sum = exponentialHistogramMetricPoint.Sum;
            dataPoint.Scale = exponentialHistogramMetricPoint.Scale;
            dataPoint.ZeroCount = (ulong)exponentialHistogramMetricPoint.ZeroCount;

            if (exponentialHistogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded))
            {
                dataPoint.Min = exponentialHistogramMetricPoint.Min;
                dataPoint.Max = exponentialHistogramMetricPoint.Max;
            }

            dataPoint.Positive = new OtlpMetrics.ExponentialHistogramDataPoint.Types.Buckets()
            {
                Offset = positiveBuckets.Offset
            };

            foreach (long count in positiveBuckets.Counts)
            {
                dataPoint.Positive.BucketCounts.Add((ulong)count);
            }

            dataPoint.Negative = new OtlpMetrics.ExponentialHistogramDataPoint.Types.Buckets()
            {
                Offset = negativeBuckets.Offset
            };

            foreach (long count in negativeBuckets.Counts)
            {
                dataPoint.Negative.BucketCounts.Add((ulong)count);
            }

            foreach (ref readonly var exemplar in exemplars)
            {
                OtlpMetrics.OtlpMetricsExtensions.AddExemplar(
                    dataPoint.Exemplars,
                    _Metric,
                    in exemplar);
            }

            _OtlpMetric.ExponentialHistogram.DataPoints.Add(dataPoint);
        }

        public override void WriteSummaryMetricPoint(
            in SummaryMetricPoint summaryMetricPoint,
            ReadOnlySpan<SummaryMetricPointQuantile> quantiles,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        {
            Debug.Assert(_OtlpMetric?.Summary != null);

            var dataPoint = new OtlpMetrics.SummaryDataPoint
            {
                StartTimeUnixNano = summaryMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds(),
                TimeUnixNano = summaryMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds(),
            };

            OtlpCommon.OtlpCommonExtensions.AddRange(dataPoint.Attributes, attributes);

            dataPoint.Count = (ulong)summaryMetricPoint.Count;
            dataPoint.Sum = summaryMetricPoint.Sum;

            foreach (ref readonly var quantile in quantiles)
            {
                dataPoint.QuantileValues.Add(
                    new OtlpMetrics.SummaryDataPoint.Types.ValueAtQuantile()
                    {
                        Quantile = quantile.Quantile,
                        Value = quantile.Value
                    });
            }

            _OtlpMetric.Summary.DataPoints.Add(dataPoint);
        }
    }
}
