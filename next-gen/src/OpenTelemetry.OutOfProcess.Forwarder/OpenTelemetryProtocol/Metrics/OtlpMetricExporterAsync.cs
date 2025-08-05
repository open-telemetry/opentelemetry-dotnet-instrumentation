// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.OpenTelemetryProtocol.Serializer;
using OpenTelemetry.Resources;

using SdkMetrics = OpenTelemetry.Metrics;

namespace OpenTelemetry.OpenTelemetryProtocol.Metrics;

/// <summary>
/// OTLP metric exporter.
/// </summary>
internal sealed class OtlpMetricExporterAsync : OtlpExporterAsync<OtlpBufferState, MetricBatchWriter>, IMetricExporterAsync
{
    private readonly OtlpMetricWriter _Writer;

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
        _Writer = new();
    }

    /// <inheritdoc/>
    public bool SupportsDeltaAggregationTemporality => true;

    /// <inheritdoc/>
    public override Task<bool> ExportAsync<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
    {
        OtlpMetricWriter writer = _Writer;

        if (!batch.WriteTo(writer))
        {
            writer.Reset();
            return Task.FromResult(false);
        }

        return SendAsync(writer, cancellationToken);
    }

    internal sealed class OtlpMetricWriter : MetricBatchWriter, IOtlpBatchWriter<OtlpBufferState>
    {
        private const int TraceIdSize = 16;
        private const int SpanIdSize = 8;
        private readonly OtlpBufferState _BufferState;
        private int _MetricsDataResourceMetricsLengthPosition;
        private int _ResourceMetricsScopeMetricsLengthPosition;
        private int _ScopeMetricsMetricsLengthPosition;
        private int _MetricDataSumLengthPosition;
        private int _MetricDataGaugeLengthPosition;
        private int _MetricDataHistogramLengthPosition;
        private int _MetricDataExponentialHistogramLengthPosition;
        private int _MetricDataSummaryLengthPosition;
        private Metric? _Metric;

        public OtlpMetricWriter()
        {
            _BufferState = new OtlpBufferState();
        }

        public OtlpBufferState Request => _BufferState;

        public void Reset()
        {
            _BufferState.Reset();
            _MetricsDataResourceMetricsLengthPosition = 0;
            _ResourceMetricsScopeMetricsLengthPosition = 0;
            _ScopeMetricsMetricsLengthPosition = 0;
            _MetricDataSumLengthPosition = 0;
            _MetricDataGaugeLengthPosition = 0;
            _MetricDataHistogramLengthPosition = 0;
            _MetricDataExponentialHistogramLengthPosition = 0;
            _MetricDataSummaryLengthPosition = 0;
            _Metric = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, 0, ProtobufOtlpMetricFieldNumberConstants.MetricsData_Resource_Metrics, ProtobufWireType.LEN);
            _MetricsDataResourceMetricsLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = _BufferState.Buffer,
                WritePosition = _BufferState.WritePosition,
            };

            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ResourceMetrics_Resource, ProtobufWireType.LEN);
            int resourceLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            foreach (KeyValuePair<string, object> attribute in resource.Attributes)
            {
                ProcessResourceAttribute(ref otlpTagWriterState, attribute);
            }

            int resourceLength = otlpTagWriterState.WritePosition - (resourceLengthPosition + OtlpBufferState.ReserveSizeForLength);
            ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, resourceLengthPosition, resourceLength);

            _BufferState.WritePosition = otlpTagWriterState.WritePosition;
        }

        public override void EndBatch() => ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _MetricsDataResourceMetricsLengthPosition, _BufferState.WritePosition - (_MetricsDataResourceMetricsLengthPosition + OtlpBufferState.ReserveSizeForLength));

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ResourceMetrics_Scope_Metrics, ProtobufWireType.LEN);
            _ResourceMetricsScopeMetricsLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ScopeMetrics_Scope, ProtobufWireType.LEN);
            int instrumentationScopeLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Name, instrumentationScope.Name);
            if (instrumentationScope.Version != null)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Version, instrumentationScope.Version);
            }

            if (instrumentationScope.Attributes != null)
            {
                var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
                {
                    Buffer = _BufferState.Buffer,
                    WritePosition = _BufferState.WritePosition,
                };

                for (int i = 0; i < instrumentationScope.Attributes.Count; i++)
                {
                    otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Attributes, ProtobufWireType.LEN);
                    int instrumentationScopeAttributesLengthPosition = otlpTagWriterState.WritePosition;
                    otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                    ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, instrumentationScope.Attributes[i].Key, instrumentationScope.Attributes[i].Value);

                    int instrumentationScopeAttributesLength = otlpTagWriterState.WritePosition - (instrumentationScopeAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength);
                    ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, instrumentationScopeAttributesLengthPosition, instrumentationScopeAttributesLength);
                }

                _BufferState.WritePosition = otlpTagWriterState.WritePosition;
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, instrumentationScopeLengthPosition, _BufferState.WritePosition - (instrumentationScopeLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        public override void EndInstrumentationScope() => ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _ResourceMetricsScopeMetricsLengthPosition, _BufferState.WritePosition - (_ResourceMetricsScopeMetricsLengthPosition + OtlpBufferState.ReserveSizeForLength));

        public override void BeginMetric(Metric metric)
        {
            Debug.Assert(metric != null);
            _Metric = metric;

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ScopeMetrics_Metrics, ProtobufWireType.LEN);
            _ScopeMetricsMetricsLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Name, metric.Name);

            if (metric.Description != null)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Description, metric.Description);
            }

            if (metric.Unit != null)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Unit, metric.Unit);
            }
        }

        public override void EndMetric()
        {
            if (_MetricDataSumLengthPosition != 0)
            {
                ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _MetricDataSumLengthPosition, _BufferState.WritePosition - (_MetricDataSumLengthPosition + OtlpBufferState.ReserveSizeForLength));
                _MetricDataSumLengthPosition = 0;
            }

            if (_MetricDataGaugeLengthPosition != 0)
            {
                ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _MetricDataGaugeLengthPosition, _BufferState.WritePosition - (_MetricDataGaugeLengthPosition + OtlpBufferState.ReserveSizeForLength));
                _MetricDataGaugeLengthPosition = 0;
            }

            if (_MetricDataHistogramLengthPosition != 0)
            {
                ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _MetricDataHistogramLengthPosition, _BufferState.WritePosition - (_MetricDataHistogramLengthPosition + OtlpBufferState.ReserveSizeForLength));
                _MetricDataHistogramLengthPosition = 0;
            }

            if (_MetricDataExponentialHistogramLengthPosition != 0)
            {
                ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _MetricDataExponentialHistogramLengthPosition, _BufferState.WritePosition - (_MetricDataExponentialHistogramLengthPosition + OtlpBufferState.ReserveSizeForLength));
                _MetricDataExponentialHistogramLengthPosition = 0;
            }

            if (_MetricDataSummaryLengthPosition != 0)
            {
                ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _MetricDataSummaryLengthPosition, _BufferState.WritePosition - (_MetricDataSummaryLengthPosition + OtlpBufferState.ReserveSizeForLength));
                _MetricDataSummaryLengthPosition = 0;
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _ScopeMetricsMetricsLengthPosition, _BufferState.WritePosition - (_ScopeMetricsMetricsLengthPosition + OtlpBufferState.ReserveSizeForLength));

            _Metric = null;
        }

        public override void WriteNumberMetricPoint(
            in NumberMetricPoint numberMetricPoint,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ReadOnlySpan<Exemplar> exemplars)
        {
            Debug.Assert(_Metric != null);

            int tagPosition;

            if (_Metric.MetricType == MetricType.LongSum ||
                _Metric.MetricType == MetricType.LongSumNonMonotonic ||
                _Metric.MetricType == MetricType.DoubleSum ||
                _Metric.MetricType == MetricType.DoubleSumNonMonotonic)
            {
                tagPosition = ProtobufOtlpMetricFieldNumberConstants.Sum_Data_Points;

                if (_MetricDataSumLengthPosition == 0)
                {
                    int aggregationTemporality = (int)_Metric.AggregationTemporality;

                    _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Data_Sum, ProtobufWireType.LEN);
                    _MetricDataSumLengthPosition = _BufferState.WritePosition;
                    _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                    _BufferState.WritePosition = ProtobufSerializer.WriteBoolWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Sum_Is_Monotonic, !_Metric.IsSumNonMonotonic);
                    _BufferState.WritePosition = ProtobufSerializer.WriteEnumWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Sum_Aggregation_Temporality, aggregationTemporality);
                }
            }
            else if (_Metric.MetricType == MetricType.LongGauge ||
                     _Metric.MetricType == MetricType.DoubleGauge)
            {
                tagPosition = ProtobufOtlpMetricFieldNumberConstants.Gauge_Data_Points;

                if (_MetricDataGaugeLengthPosition == 0)
                {
                    _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Data_Gauge, ProtobufWireType.LEN);
                    _MetricDataGaugeLengthPosition = _BufferState.WritePosition;
                    _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;
                }
            }
            else
            {
                throw new NotSupportedException($"Unsupported metric type for number data point: {_Metric.MetricType}");
            }

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, tagPosition, ProtobufWireType.LEN);
            int dataPointLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = WriteAttributes(_BufferState.Buffer, _BufferState.WritePosition, attributes, ProtobufOtlpMetricFieldNumberConstants.NumberDataPoint_Attributes);

            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.NumberDataPoint_Start_Time_Unix_Nano, numberMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.NumberDataPoint_Time_Unix_Nano, numberMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds());

            if (!_Metric.IsFloatingPoint)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.NumberDataPoint_Value_As_Int, (ulong)numberMetricPoint.ValueAsLong);
            }
            else
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.NumberDataPoint_Value_As_Double, numberMetricPoint.ValueAsDouble);
            }

            foreach (ref readonly Exemplar exemplar in exemplars)
            {
                _BufferState.WritePosition = WriteExemplar(_BufferState.Buffer, _BufferState.WritePosition, exemplar, ProtobufOtlpMetricFieldNumberConstants.NumberDataPoint_Exemplars);
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, dataPointLengthPosition, _BufferState.WritePosition - (dataPointLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        public override void WriteHistogramMetricPoint(
            in HistogramMetricPoint histogramMetricPoint,
            ReadOnlySpan<HistogramMetricPointBucket> buckets,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ReadOnlySpan<Exemplar> exemplars)
        {
            Debug.Assert(_Metric != null);
            Debug.Assert(_Metric.MetricType == MetricType.Histogram);

            int aggregationTemporality = (int)_Metric.AggregationTemporality;

            if (_MetricDataHistogramLengthPosition == 0)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Data_Histogram, ProtobufWireType.LEN);
                _MetricDataHistogramLengthPosition = _BufferState.WritePosition;
                _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                _BufferState.WritePosition = ProtobufSerializer.WriteEnumWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Histogram_Aggregation_Temporality, aggregationTemporality);
            }

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Histogram_Data_Points, ProtobufWireType.LEN);
            int dataPointLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = WriteAttributes(_BufferState.Buffer, _BufferState.WritePosition, attributes, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Attributes);

            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Start_Time_Unix_Nano, histogramMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Time_Unix_Nano, histogramMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Count, (ulong)histogramMetricPoint.Count);
            _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Sum, histogramMetricPoint.Sum);

            if (histogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded))
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Min, histogramMetricPoint.Min);
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Max, histogramMetricPoint.Max);
            }

            if (histogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.BucketsRecorded))
            {
                foreach (ref readonly HistogramMetricPointBucket bucket in buckets)
                {
                    if (!double.IsPositiveInfinity(bucket.Value))
                    {
                        _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Explicit_Bounds, bucket.Value);
                    }

                    _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Bucket_Counts, (ulong)bucket.Count);
                }
            }

            foreach (ref readonly Exemplar exemplar in exemplars)
            {
                _BufferState.WritePosition = WriteExemplar(_BufferState.Buffer, _BufferState.WritePosition, exemplar, ProtobufOtlpMetricFieldNumberConstants.HistogramDataPoint_Exemplars);
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, dataPointLengthPosition, _BufferState.WritePosition - (dataPointLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        public override void WriteExponentialHistogramMetricPoint(
            in ExponentialHistogramMetricPoint exponentialHistogramMetricPoint,
            in ExponentialHistogramMetricPointBuckets positiveBuckets,
            in ExponentialHistogramMetricPointBuckets negativeBuckets,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ReadOnlySpan<Exemplar> exemplars)
        {
            Debug.Assert(_Metric != null);
            Debug.Assert(_Metric.MetricType == MetricType.ExponentialHistogram);

            if (_MetricDataExponentialHistogramLengthPosition == 0)
            {
                int aggregationTemporality = (int)_Metric.AggregationTemporality;
                _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Data_Exponential_Histogram, ProtobufWireType.LEN);
                _MetricDataExponentialHistogramLengthPosition = _BufferState.WritePosition;
                _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                _BufferState.WritePosition = ProtobufSerializer.WriteEnumWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogram_Aggregation_Temporality, aggregationTemporality);
            }

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogram_Data_Points, ProtobufWireType.LEN);
            int dataPointLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = WriteAttributes(_BufferState.Buffer, _BufferState.WritePosition, attributes, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Attributes);

            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Start_Time_Unix_Nano, exponentialHistogramMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Time_Unix_Nano, exponentialHistogramMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Count, (ulong)exponentialHistogramMetricPoint.Count);
            _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Sum, exponentialHistogramMetricPoint.Sum);
            _BufferState.WritePosition = ProtobufSerializer.WriteSInt32WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Scale, exponentialHistogramMetricPoint.Scale);
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Zero_Count, (ulong)exponentialHistogramMetricPoint.ZeroCount);

            if (exponentialHistogramMetricPoint.Features.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded))
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Min, exponentialHistogramMetricPoint.Min);
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Max, exponentialHistogramMetricPoint.Max);
            }

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Positive, ProtobufWireType.LEN);
            int positiveBucketsLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteSInt32WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Buckets_Offset, positiveBuckets.Offset);

            for (int i = 0; i < positiveBuckets.Counts.Length; i++)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteInt64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Buckets_Bucket_Counts, (ulong)positiveBuckets.Counts[i]);
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, positiveBucketsLengthPosition, _BufferState.WritePosition - (positiveBucketsLengthPosition + OtlpBufferState.ReserveSizeForLength));

            foreach (ref readonly Exemplar exemplar in exemplars)
            {
                _BufferState.WritePosition = WriteExemplar(_BufferState.Buffer, _BufferState.WritePosition, exemplar, ProtobufOtlpMetricFieldNumberConstants.ExponentialHistogramDataPoint_Exemplars);
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, dataPointLengthPosition, _BufferState.WritePosition - (dataPointLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        public override void WriteSummaryMetricPoint(
            in SummaryMetricPoint summaryMetricPoint,
            ReadOnlySpan<SummaryMetricPointQuantile> quantiles,
            ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        {
            Debug.Assert(_Metric != null);
            Debug.Assert(_Metric.MetricType == MetricType.Summary);

            if (_MetricDataSummaryLengthPosition == 0)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Metric_Data_Summary, ProtobufWireType.LEN);
                _MetricDataSummaryLengthPosition = _BufferState.WritePosition;
                _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;
            }

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.Summary_Data_Points, ProtobufWireType.LEN);
            int dataPointLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = WriteAttributes(_BufferState.Buffer, _BufferState.WritePosition, attributes, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_Attributes);
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_Start_Time_Unix_Nano, summaryMetricPoint.StartTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_Time_Unix_Nano, summaryMetricPoint.EndTimeUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_Count, (ulong)summaryMetricPoint.Count);
            _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_Sum, summaryMetricPoint.Sum);

            foreach (ref readonly SummaryMetricPointQuantile quantile in quantiles)
            {
                // Since a quantile value message always contains exactly two doubles (quantile + value),
                // Each double field takes 9 bytes (1 for tag, 8 for value)
                // Two double fields = 18 bytes total for the message body
                _BufferState.WritePosition = ProtobufSerializer.WriteTagAndLength(_BufferState.Buffer, _BufferState.WritePosition, 18, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_Quantile_Values, ProtobufWireType.LEN);
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_ValueAtQuantiles_Quantile, quantile.Quantile);
                _BufferState.WritePosition = ProtobufSerializer.WriteDoubleWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpMetricFieldNumberConstants.SummaryDataPoint_ValueAtQuantiles_Value, quantile.Value);
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, dataPointLengthPosition, _BufferState.WritePosition - (dataPointLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        private int WriteExemplar(byte[] buffer, int writePosition, Exemplar exemplar, int exemplarsFieldNumber)
        {
            Debug.Assert(_Metric != null);

            writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, exemplarsFieldNumber, ProtobufWireType.LEN);
            int exemplarLengthPosition = writePosition;
            writePosition += OtlpBufferState.ReserveSizeForLength;

            ref readonly TagList filteredAttributes = ref SdkMetrics.Exemplar.GetFilteredAttributesReference(in exemplar);
            writePosition = WriteTagList(buffer, writePosition, filteredAttributes, ProtobufOtlpMetricFieldNumberConstants.Exemplar_Filtered_Attributes);

            writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, ProtobufOtlpMetricFieldNumberConstants.Exemplar_Time_Unix_Nano, exemplar.TimestampUtc.ToUnixTimeNanoseconds());

            if (!_Metric.IsFloatingPoint)
            {
                writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, ProtobufOtlpMetricFieldNumberConstants.Exemplar_Value_As_Int, (ulong)exemplar.ValueAsLong);
            }
            else
            {
                writePosition = ProtobufSerializer.WriteDoubleWithTag(buffer, writePosition, ProtobufOtlpMetricFieldNumberConstants.Exemplar_Value_As_Double, exemplar.ValueAsDouble);
            }

            if (exemplar.SpanId != default)
            {
                writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, SpanIdSize, ProtobufOtlpMetricFieldNumberConstants.Exemplar_Span_Id, ProtobufWireType.LEN);
                var spanIdBytes = new Span<byte>(buffer, writePosition, SpanIdSize);
                exemplar.SpanId.CopyTo(spanIdBytes);
                writePosition += SpanIdSize;

                writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, TraceIdSize, ProtobufOtlpMetricFieldNumberConstants.Exemplar_Trace_Id, ProtobufWireType.LEN);
                var traceIdBytes = new Span<byte>(buffer, writePosition, TraceIdSize);
                exemplar.TraceId.CopyTo(traceIdBytes);
                writePosition += TraceIdSize;
            }

            ProtobufSerializer.WriteReservedLength(buffer, exemplarLengthPosition, writePosition - (exemplarLengthPosition + OtlpBufferState.ReserveSizeForLength));
            return writePosition;
        }

        private static void ProcessResourceAttribute(ref ProtobufOtlpTagWriter.OtlpTagWriterState otlpTagWriterState, KeyValuePair<string, object> attribute)
        {
            // Use ResourceSpans_Resource_Attributes since there's no specific metric constant for this
            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Resource_Attributes, ProtobufWireType.LEN);
            int resourceAttributesLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, attribute.Key, attribute.Value);

            int resourceAttributesLength = otlpTagWriterState.WritePosition - (resourceAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength);
            ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, resourceAttributesLengthPosition, resourceAttributesLength);
        }

        private static int WriteAttributes(byte[] buffer, int writePosition, ReadOnlySpan<KeyValuePair<string, object?>> tags, int attributesFieldNumber)
        {
            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = buffer,
                WritePosition = writePosition,
            };

            foreach (ref readonly KeyValuePair<string, object?> tag in tags)
            {
                otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, attributesFieldNumber, ProtobufWireType.LEN);
                int attributeLengthPosition = otlpTagWriterState.WritePosition;
                otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, attributeLengthPosition, otlpTagWriterState.WritePosition - (attributeLengthPosition + OtlpBufferState.ReserveSizeForLength));
                otlpTagWriterState.TagCount++;
            }

            return otlpTagWriterState.WritePosition;
        }

        private static int WriteTagList(byte[] buffer, int writePosition, TagList tags, int attributesFieldNumber)
        {
            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = buffer,
                WritePosition = writePosition,
            };

            foreach (var tag in tags)
            {
                otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, attributesFieldNumber, ProtobufWireType.LEN);
                int attributeLengthPosition = otlpTagWriterState.WritePosition;
                otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, attributeLengthPosition, otlpTagWriterState.WritePosition - (attributeLengthPosition + OtlpBufferState.ReserveSizeForLength));
                otlpTagWriterState.TagCount++;
            }

            return otlpTagWriterState.WritePosition;
        }
    }
}
