// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.OpenTelemetryProtocol.Serializer;
using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

namespace OpenTelemetry.OpenTelemetryProtocol.Tracing;

/// <summary>
/// OTLP span exporter.
/// </summary>
internal sealed class OtlpSpanExporterAsync : OtlpExporterAsync<OtlpBufferState, SpanBatchWriter>, ISpanExporterAsync
{
    private readonly OtlpSpanWriter _Writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpSpanExporterAsync"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    public OtlpSpanExporterAsync(
        ILogger<OtlpSpanExporterAsync> logger,
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

    internal sealed class OtlpSpanWriter : SpanBatchWriter, IOtlpBatchWriter<OtlpBufferState>
    {
        private const int TraceIdSize = 16;
        private const int SpanIdSize = 8;
        private readonly OtlpBufferState _BufferState;
        private int _TracesDataResourceSpansLengthPosition;
        private int _ResourceSpansScopeSpansLengthPosition;

        public OtlpSpanWriter()
        {
            _BufferState = new OtlpBufferState();
        }

        public OtlpBufferState Request => _BufferState;

        public void Reset()
        {
            _TracesDataResourceSpansLengthPosition = 0;
            _ResourceSpansScopeSpansLengthPosition = 0;
            _BufferState.Reset();
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            _BufferState.WritePosition = ProtobufSerializer.WriteTag(Request.Buffer, 0, ProtobufOtlpTraceFieldNumberConstants.TracesData_Resource_Spans, ProtobufWireType.LEN);
            _TracesDataResourceSpansLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = this.Request.Buffer,
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

        public override void EndBatch() => ProtobufSerializer.WriteReservedLength(Request.Buffer, _TracesDataResourceSpansLengthPosition, _BufferState.WritePosition - (_TracesDataResourceSpansLengthPosition + OtlpBufferState.ReserveSizeForLength));

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.ResourceSpans_Scope_Spans, ProtobufWireType.LEN);
            _ResourceSpansScopeSpansLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.ScopeSpans_Scope, ProtobufWireType.LEN);
            int instrumentationScopeLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Name, instrumentationScope.Name);
            if (instrumentationScope.Version != null)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Version, instrumentationScope.Version);
            }

            if (instrumentationScope.Attributes != null)
            {
                var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
                {
                    Buffer = this.Request.Buffer,
                    WritePosition = this._BufferState.WritePosition,
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

            ProtobufSerializer.WriteReservedLength(Request.Buffer, instrumentationScopeLengthPosition, _BufferState.WritePosition - (instrumentationScopeLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        public override void EndInstrumentationScope() => ProtobufSerializer.WriteReservedLength(Request.Buffer, _ResourceSpansScopeSpansLengthPosition, _BufferState.WritePosition - (_ResourceSpansScopeSpansLengthPosition + OtlpBufferState.ReserveSizeForLength));

        public override void WriteSpan(in Span span)
        {
            _BufferState.WritePosition = ProtobufSerializer.WriteTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.ScopeSpans_Span, ProtobufWireType.LEN);
            int spanLengthPosition = _BufferState.WritePosition;
            _BufferState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            _BufferState.WritePosition = ProtobufSerializer.WriteTagAndLength(Request.Buffer, _BufferState.WritePosition, TraceIdSize, ProtobufOtlpTraceFieldNumberConstants.Span_Trace_Id, ProtobufWireType.LEN);
            _BufferState.WritePosition = WriteTraceId(Request.Buffer, _BufferState.WritePosition, span.Info.TraceId);

            _BufferState.WritePosition = ProtobufSerializer.WriteTagAndLength(Request.Buffer, _BufferState.WritePosition, SpanIdSize, ProtobufOtlpTraceFieldNumberConstants.Span_Span_Id, ProtobufWireType.LEN);
            _BufferState.WritePosition = WriteSpanId(Request.Buffer, _BufferState.WritePosition, span.Info.SpanId);

            if (!string.IsNullOrEmpty(span.Info.TraceState))
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Trace_State, span.Info.TraceState);
            }

            if (span.Info.ParentSpanId != default)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteTagAndLength(Request.Buffer, _BufferState.WritePosition, SpanIdSize, ProtobufOtlpTraceFieldNumberConstants.Span_Parent_Span_Id, ProtobufWireType.LEN);
                _BufferState.WritePosition = WriteSpanId(Request.Buffer, _BufferState.WritePosition, span.Info.ParentSpanId);
            }

            _BufferState.WritePosition = WriteSpanFlags(Request.Buffer, _BufferState.WritePosition, span.Info.TraceFlags, ProtobufOtlpTraceFieldNumberConstants.Span_Flags);

            _BufferState.WritePosition = ProtobufSerializer.WriteStringWithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Name, span.Info.Name);

            if (span.Info.Kind.HasValue)
            {
                _BufferState.WritePosition = ProtobufSerializer.WriteEnumWithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Kind, (int)span.Info.Kind + 1);
            }

            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Start_Time_Unix_Nano, span.Info.StartTimestampUtc.ToUnixTimeNanoseconds());
            _BufferState.WritePosition = ProtobufSerializer.WriteFixed64WithTag(Request.Buffer, _BufferState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_End_Time_Unix_Nano, span.Info.EndTimestampUtc.ToUnixTimeNanoseconds());

            _BufferState.WritePosition = WriteAttributes(Request.Buffer, _BufferState.WritePosition, span.Attributes);
            _BufferState.WritePosition = WriteSpanEvents(Request.Buffer, _BufferState.WritePosition, span.Events);
            _BufferState.WritePosition = WriteSpanLinks(Request.Buffer, _BufferState.WritePosition, span.Links);
            _BufferState.WritePosition = WriteSpanStatus(Request.Buffer, _BufferState.WritePosition, span.Info.StatusCode, span.Info.StatusDescription);

            ProtobufSerializer.WriteReservedLength(Request.Buffer, spanLengthPosition, _BufferState.WritePosition - (spanLengthPosition + OtlpBufferState.ReserveSizeForLength));
        }

        internal static void ProcessResourceAttribute(ref ProtobufOtlpTagWriter.OtlpTagWriterState otlpTagWriterState, KeyValuePair<string, object> attribute)
        {
            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Resource_Attributes, ProtobufWireType.LEN);
            int resourceAttributesLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

            ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, attribute.Key, attribute.Value);

            int resourceAttributesLength = otlpTagWriterState.WritePosition - (resourceAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength);
            ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, resourceAttributesLengthPosition, resourceAttributesLength);
        }

        internal static int WriteTraceId(byte[] buffer, int position, ActivityTraceId activityTraceId)
        {
            var traceBytes = new Span<byte>(buffer, position, TraceIdSize);
            activityTraceId.CopyTo(traceBytes);
            return position + TraceIdSize;
        }

        internal static int WriteSpanId(byte[] buffer, int position, ActivitySpanId activitySpanId)
        {
            var spanIdBytes = new Span<byte>(buffer, position, SpanIdSize);
            activitySpanId.CopyTo(spanIdBytes);
            return position + SpanIdSize;
        }

        internal static int WriteSpanFlags(byte[] buffer, int position, ActivityTraceFlags activityTraceFlags, int fieldNumber)
        {
            uint spanFlags = (uint)activityTraceFlags & (byte)0x000000FF;
            spanFlags |= 0x00000100;
            position = ProtobufSerializer.WriteFixed32WithTag(buffer, position, fieldNumber, spanFlags);
            return position;
        }

        internal static int WriteAttributes(byte[] buffer, int writePosition, ReadOnlySpan<KeyValuePair<string, object?>> tags)
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
                otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Attributes, ProtobufWireType.LEN);
                int spanAttributesLengthPosition = otlpTagWriterState.WritePosition;
                otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, spanAttributesLengthPosition, otlpTagWriterState.WritePosition - (spanAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength));
                otlpTagWriterState.TagCount++;
            }

            return otlpTagWriterState.WritePosition;
        }

        internal static int WriteSpanEvents(byte[] buffer, int writePosition, ReadOnlySpan<SpanEvent> events)
        {
            foreach (ref readonly var evnt in events)
            {
                writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Events, ProtobufWireType.LEN);
                int spanEventsLengthPosition = writePosition;
                writePosition += OtlpBufferState.ReserveSizeForLength; // Reserve 4 bytes for length

                writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Event_Name, evnt.Name);
                writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Event_Time_Unix_Nano, (ulong)evnt.TimestampUtc.ToUnixTimeNanoseconds());

                ref readonly var attributes = ref SpanEvent.GetAttributesReference(in evnt);

                writePosition = WriteEventAttributes(buffer, writePosition, attributes);
                ProtobufSerializer.WriteReservedLength(buffer, spanEventsLengthPosition, writePosition - (spanEventsLengthPosition + OtlpBufferState.ReserveSizeForLength));
            }

            return writePosition;
        }

        internal static int WriteEventAttributes(byte[] buffer, int writePosition, in TagList attributes)
        {
            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = buffer,
                WritePosition = writePosition,
                TagCount = 0,
                DroppedTagCount = 0,
            };

            foreach (var tag in attributes)
            {
                otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Event_Attributes, ProtobufWireType.LEN);
                int eventAttributesLengthPosition = otlpTagWriterState.WritePosition;
                otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, eventAttributesLengthPosition, otlpTagWriterState.WritePosition - (eventAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength));
                otlpTagWriterState.TagCount++;
            }

            return otlpTagWriterState.WritePosition;
        }

        internal static int WriteSpanLinks(byte[] buffer, int writePosition, ReadOnlySpan<SpanLink> links)
        {
            foreach (ref readonly var link in links)
            {
                ref readonly var context = ref SpanLink.GetSpanContextReference(in link);
                ref readonly var attributes = ref SpanLink.GetAttributesReference(in link);

                writePosition = ProtobufSerializer.WriteTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Links, ProtobufWireType.LEN);
                int spanLinksLengthPosition = writePosition;
                writePosition += OtlpBufferState.ReserveSizeForLength;

                writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, TraceIdSize, ProtobufOtlpTraceFieldNumberConstants.Link_Trace_Id, ProtobufWireType.LEN);
                writePosition = WriteTraceId(buffer, writePosition, context.TraceId);

                writePosition = ProtobufSerializer.WriteTagAndLength(buffer, writePosition, SpanIdSize, ProtobufOtlpTraceFieldNumberConstants.Link_Span_Id, ProtobufWireType.LEN);
                writePosition = WriteSpanId(buffer, writePosition, context.SpanId);

                if (!string.IsNullOrEmpty(context.TraceState))
                {
                    writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Link_Trace_State, context.TraceState);
                }

                writePosition = WriteLinkAttributes(buffer, writePosition, in attributes);
                writePosition = WriteSpanFlags(buffer, writePosition, context.TraceFlags, ProtobufOtlpTraceFieldNumberConstants.Link_Flags);

                ProtobufSerializer.WriteReservedLength(buffer, spanLinksLengthPosition, writePosition - (spanLinksLengthPosition + OtlpBufferState.ReserveSizeForLength));
            }

            return writePosition;
        }

        internal static int WriteLinkAttributes(byte[] buffer, int writePosition, in TagList attributes)
        {
            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = buffer,
                WritePosition = writePosition,
                TagCount = 0,
                DroppedTagCount = 0,
            };

            foreach (var tag in attributes)
            {
                otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Link_Attributes, ProtobufWireType.LEN);
                int linkAttributesLengthPosition = otlpTagWriterState.WritePosition;
                otlpTagWriterState.WritePosition += OtlpBufferState.ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, linkAttributesLengthPosition, otlpTagWriterState.WritePosition - (linkAttributesLengthPosition + OtlpBufferState.ReserveSizeForLength));
            }

            return otlpTagWriterState.WritePosition;
        }

        private static int WriteSpanStatus(byte[] buffer, int position, ActivityStatusCode statusCode, string? statusDescription)
        {
            if (statusCode == ActivityStatusCode.Unset)
            {
                return position;
            }

            if (statusCode == ActivityStatusCode.Error && statusDescription != null)
            {
                var descriptionSpan = statusDescription.AsSpan();
                int numberOfUtf8CharsInString = ProtobufSerializer.GetNumberOfUtf8CharsInString(descriptionSpan);
                int serializedLengthSize = ProtobufSerializer.ComputeVarInt64Size((ulong)numberOfUtf8CharsInString);

                // length = numberOfUtf8CharsInString + Status_Message tag size + serializedLengthSize field size + Span_Status tag size + Span_Status length size.
                position = ProtobufSerializer.WriteTagAndLength(buffer, position, numberOfUtf8CharsInString + 1 + serializedLengthSize + 2, ProtobufOtlpTraceFieldNumberConstants.Span_Status, ProtobufWireType.LEN);
                position = ProtobufSerializer.WriteStringWithTag(buffer, position, ProtobufOtlpTraceFieldNumberConstants.Status_Message, numberOfUtf8CharsInString, descriptionSpan);
            }
            else
            {
                position = ProtobufSerializer.WriteTagAndLength(buffer, position, 2, ProtobufOtlpTraceFieldNumberConstants.Span_Status, ProtobufWireType.LEN);
            }

            position = ProtobufSerializer.WriteEnumWithTag(buffer, position, ProtobufOtlpTraceFieldNumberConstants.Status_Code, (int)statusCode);

            return position;
        }
    }
}
