// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Logging;
using OpenTelemetry.OpenTelemetryProtocol.Serializer;
using OpenTelemetry.Resources;

namespace OpenTelemetry.OpenTelemetryProtocol.Logging;

/// <summary>
/// OTLP log record exporter.
/// </summary>
internal sealed class OtlpLogRecordExporterAsync : OtlpExporterAsync<OtlpBufferState, LogRecordBatchWriter>, ILogRecordExporterAsync
{
    private readonly OtlpLogRecordWriter _Writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpLogRecordExporterAsync"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    public OtlpLogRecordExporterAsync(
        ILogger<OtlpLogRecordExporterAsync> logger,
        OtlpExporterOptions options)
        : base(logger, options)
    {
        _Writer = new();
    }

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

    private sealed class OtlpLogRecordWriter : LogRecordBatchWriter, IOtlpBatchWriter<OtlpBufferState>
    {
        private const int TraceIdSize = 16;
        private const int SpanIdSize = 8;
        private readonly OtlpBufferState _BufferState;
        private int _LogsDataResourceLogsLengthPosition;
        private int _ResourceLogsScopeLogsLengthPosition;

        public OtlpLogRecordWriter()
        {
            _BufferState = new OtlpBufferState();
        }

        public OtlpBufferState Request => _BufferState;

        public void Reset()
        {
            _BufferState.Reset();
            _LogsDataResourceLogsLengthPosition = 0;
            _ResourceLogsScopeLogsLengthPosition = 0;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, 0, ProtobufOtlpLogFieldNumberConstants.LogsData_Resource_Logs, ProtobufWireType.LEN);
            _LogsDataResourceLogsLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = _BufferState.Buffer,
                WritePosition = this._BufferState.WritePosition,
            };

            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.ResourceSpans_Resource, ProtobufWireType.LEN);
            int resourceLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            foreach (var attribute in resource.Attributes)
            {
                ProcessResourceAttribute(ref otlpTagWriterState, attribute);
            }

            int resourceLength = otlpTagWriterState.WritePosition - (resourceLengthPosition + OtlpBufferState.ReserveSizeForLength);
            ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, resourceLengthPosition, resourceLength);

            _BufferState.WritePosition = otlpTagWriterState.WritePosition;
        }

        public override void EndBatch() => ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _LogsDataResourceLogsLengthPosition, _BufferState.WritePosition - (_LogsDataResourceLogsLengthPosition + OtlpBufferState.ReserveSizeForLength));

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.ResourceLogs_Scope_Logs, ProtobufWireType.LEN);
            _ResourceLogsScopeLogsLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.ScopeLogs_Scope, ProtobufWireType.LEN);
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
                    TagCount = 0,
                    DroppedTagCount = 0,
                };

                for (int i = 0; i < instrumentationScope.Attributes.Count; i++)
                {
                    otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Attributes, ProtobufWireType.LEN);
                    int instrumentationScopeAttributesLengthPosition = otlpTagWriterState.WritePosition;
                    otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                    ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, instrumentationScope.Attributes[i].Key, instrumentationScope.Attributes[i].Value);

                    var instrumentationScopeAttributesLength = otlpTagWriterState.WritePosition - (instrumentationScopeAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength);
                    ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, instrumentationScopeAttributesLengthPosition, instrumentationScopeAttributesLength);
                }

                _BufferState.WritePosition = otlpTagWriterState.WritePosition;
            }

            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, instrumentationScopeLengthPosition, _BufferState.WritePosition - (instrumentationScopeLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        public override void EndInstrumentationScope() => ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, _ResourceLogsScopeLogsLengthPosition, _BufferState.WritePosition - (_ResourceLogsScopeLogsLengthPosition + OtlpBufferState.ReserveSizeForLength));

        public override void WriteLogRecord(in LogRecord logRecord)
        {
            _BufferState.WritePosition = ProtobufSerializer.WriteTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.ScopeLogs_Log_Records, ProtobufWireType.LEN);
            int logRecordLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            var timestamp = logRecord.Info.TimestampUtc.ToUnixTimeNanoseconds();
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.LogRecord_Time_Unix_Nano, timestamp);
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.LogRecord_Observed_Time_Unix_Nano, timestamp);
            _BufferState.WritePosition = ProtobufSerializer.WriteEnumWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.LogRecord_Severity_Number, (int)logRecord.Info.Severity);

            if (!string.IsNullOrWhiteSpace(logRecord.Info.SeverityText))
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.LogRecord_Severity_Text, logRecord.Info.SeverityText);
            }

            ref readonly var spanContext = ref logRecord.SpanContext;

            if (spanContext.TraceId != default && spanContext.SpanId != default)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteTagAndLength(_BufferState.Buffer, _BufferState.WritePosition, TraceIdSize, ProtobufOtlpLogFieldNumberConstants.LogRecord_Trace_Id, ProtobufWireType.LEN);
                _BufferState.WritePosition = WriteTraceId(_BufferState.Buffer, _BufferState.WritePosition, spanContext.TraceId);
                _BufferState.WritePosition = ProtobufSerializer.WriteTagAndLength(_BufferState.Buffer, _BufferState.WritePosition, SpanIdSize, ProtobufOtlpLogFieldNumberConstants.LogRecord_Span_Id, ProtobufWireType.LEN);
                _BufferState.WritePosition = WriteSpanId(_BufferState.Buffer, _BufferState.WritePosition, spanContext.SpanId);
                _BufferState.WritePosition = ProtobufSerializer.WriteFixed32WithTag(_BufferState.Buffer, _BufferState.WritePosition, ProtobufOtlpLogFieldNumberConstants.LogRecord_Flags, (uint)spanContext.TraceFlags);
            }

            if (!string.IsNullOrEmpty(logRecord.Info.Body))
            {
                _BufferState.WritePosition = WriteLogRecordBody(_BufferState.Buffer, _BufferState.WritePosition, logRecord.Info.Body.AsSpan());
            }

            _BufferState.WritePosition = WriteAttributes(_BufferState.Buffer, _BufferState.WritePosition, logRecord.Attributes);
            ProtobufSerializer.WriteReservedLength(_BufferState.Buffer, logRecordLengthPosition, _BufferState.WritePosition - (logRecordLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        private static int WriteLogRecordBody(byte[] buffer, int writePosition, ReadOnlySpan<char> value)
        {
            var numberOfUtf8CharsInString = ProtobufSerializer.GetNumberOfUtf8CharsInString(value);
            var serializedLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)numberOfUtf8CharsInString);

            // length = numberOfUtf8CharsInString + tagSize + length field size.
            writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, numberOfUtf8CharsInString + 1 + serializedLengthSize, ProtobufOtlpLogFieldNumberConstants.LogRecord_Body, ProtobufWireType.LEN);
            writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, ProtobufOtlpCommonFieldNumberConstants.AnyValue_String_Value, numberOfUtf8CharsInString, value);
            return writePosition;
        }

        private static void ProcessResourceAttribute(ref ProtobufOtlpTagWriter.OtlpTagWriterState otlpTagWriterState, KeyValuePair<string, object> attribute)
        {
            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Resource_Attributes, ProtobufWireType.LEN);
            int resourceAttributesLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, attribute.Key, attribute.Value);

            int resourceAttributesLength = otlpTagWriterState.WritePosition - (resourceAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength);
            ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, resourceAttributesLengthPosition, resourceAttributesLength);
        }

        private static int WriteAttributes(byte[] buffer, int writePosition, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = buffer,
                WritePosition = writePosition,
                TagCount = 0,
                DroppedTagCount = 0,
            };

            foreach (ref readonly var tag in tags)
            {
                otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpLogFieldNumberConstants.LogRecord_Attributes, ProtobufWireType.LEN);
                int logAttributesLengthPosition = otlpTagWriterState.WritePosition;
                otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, logAttributesLengthPosition, otlpTagWriterState.WritePosition - (logAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength));
                otlpTagWriterState.TagCount++;
            }

            return otlpTagWriterState.WritePosition;
        }

        private static int WriteTraceId(byte[] buffer, int position, ActivityTraceId activityTraceId)
        {
            var traceBytes = new Span<byte>(buffer, position, TraceIdSize);
            activityTraceId.CopyTo(traceBytes);
            return position + TraceIdSize;
        }

        private static int WriteSpanId(byte[] buffer, int position, ActivitySpanId activitySpanId)
        {
            var spanIdBytes = new Span<byte>(buffer, position, SpanIdSize);
            activitySpanId.CopyTo(spanIdBytes);
            return position + SpanIdSize;
        }
    }
}
