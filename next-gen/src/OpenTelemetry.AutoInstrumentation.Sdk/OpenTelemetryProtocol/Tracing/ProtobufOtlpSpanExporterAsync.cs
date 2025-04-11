// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using OpenTelemetry.OpenTelemetryProtocol.Serializer;
using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

namespace OpenTelemetry.OpenTelemetryProtocol.Tracing;

/// <summary>
/// OTLP span exporter.
/// </summary>
internal sealed class ProtobufOtlpSpanExporterAsync : ProtobufOtlpExporterAsync<SpanBatchWriter>, ISpanExporterAsync
{
    private const int ReserveSizeForLength = 4;
    private const int TraceIdSize = 16;
    private const int SpanIdSize = 8;
    private readonly OtlpSpanWriter _Writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtobufOtlpSpanExporterAsync"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger{TCategoryName}"/>.</param>
    /// <param name="options"><see cref="OtlpExporterOptions"/>.</param>
    public ProtobufOtlpSpanExporterAsync(
        ILogger<ProtobufOtlpSpanExporterAsync> logger,
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

        return SendOtlpAsync(writer, cancellationToken);
    }

    internal sealed class OtlpSpanWriter : SpanBatchWriter, IProtobufWriter
    {
        private int _TracesDataResourceSpansLengthPosition;
        private int _ResourceSpansScopeSpansLengthPosition;

        public OtlpSpanWriter()
        {
            Buffer = new byte[750000];
            Reset();
        }

        public byte[] Buffer { get; private set; }

        public int WritePosition { get; private set; }

        public void Reset()
        {
            WritePosition = 0;
            _ResourceSpansScopeSpansLengthPosition = 0;
            _TracesDataResourceSpansLengthPosition = 0;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            WritePosition = ProtobufSerializer.WriteTag(Buffer, 0, ProtobufOtlpTraceFieldNumberConstants.TracesData_Resource_Spans, ProtobufWireType.LEN);
            _TracesDataResourceSpansLengthPosition = WritePosition;
            WritePosition += ReserveSizeForLength;

            var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
            {
                Buffer = this.Buffer,
                WritePosition = this.WritePosition,
            };

            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.ResourceSpans_Resource, ProtobufWireType.LEN);
            int resourceLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += ReserveSizeForLength;

            foreach (var attribute in resource.Attributes)
            {
                ProcessResourceAttribute(ref otlpTagWriterState, attribute);
            }

            int resourceLength = otlpTagWriterState.WritePosition - (resourceLengthPosition + ReserveSizeForLength);
            ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, resourceLengthPosition, resourceLength);

            WritePosition = otlpTagWriterState.WritePosition;
        }

        public override void EndBatch() => ProtobufSerializer.WriteReservedLength(Buffer, _TracesDataResourceSpansLengthPosition, WritePosition - (_TracesDataResourceSpansLengthPosition + ReserveSizeForLength));

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);

            WritePosition = ProtobufSerializer.WriteTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.ResourceSpans_Scope_Spans, ProtobufWireType.LEN);
            _ResourceSpansScopeSpansLengthPosition = WritePosition;
            WritePosition += ReserveSizeForLength;

            WritePosition = ProtobufSerializer.WriteTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.ScopeSpans_Scope, ProtobufWireType.LEN);
            int instrumentationScopeLengthPosition = WritePosition;
            WritePosition += ReserveSizeForLength;

            WritePosition = ProtobufSerializer.WriteStringWithTag(Buffer, WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Name, instrumentationScope.Name);
            if (instrumentationScope.Version != null)
            {
                WritePosition = ProtobufSerializer.WriteStringWithTag(Buffer, WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Version, instrumentationScope.Version);
            }

            if (instrumentationScope.Attributes != null)
            {
                var otlpTagWriterState = new ProtobufOtlpTagWriter.OtlpTagWriterState
                {
                    Buffer = this.Buffer,
                    WritePosition = this.WritePosition,
                    TagCount = 0,
                    DroppedTagCount = 0,
                };

                for (int i = 0; i < instrumentationScope.Attributes.Count; i++)
                {
                    otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpCommonFieldNumberConstants.InstrumentationScope_Attributes, ProtobufWireType.LEN);
                    int instrumentationScopeAttributesLengthPosition = otlpTagWriterState.WritePosition;
                    otlpTagWriterState.WritePosition += ReserveSizeForLength;

                    ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, instrumentationScope.Attributes[i].Key, instrumentationScope.Attributes[i].Value);

                    var instrumentationScopeAttributesLength = otlpTagWriterState.WritePosition - (instrumentationScopeAttributesLengthPosition + ReserveSizeForLength);
                    ProtobufSerializer.WriteReservedLength(otlpTagWriterState.Buffer, instrumentationScopeAttributesLengthPosition, instrumentationScopeAttributesLength);
                }

                WritePosition = otlpTagWriterState.WritePosition;
            }

            ProtobufSerializer.WriteReservedLength(Buffer, instrumentationScopeLengthPosition, WritePosition - (instrumentationScopeLengthPosition + ReserveSizeForLength));
        }

        public override void EndInstrumentationScope() => ProtobufSerializer.WriteReservedLength(Buffer, _ResourceSpansScopeSpansLengthPosition, WritePosition - (_ResourceSpansScopeSpansLengthPosition + ReserveSizeForLength));

        public override void WriteSpan(in Span span)
        {
            WritePosition = ProtobufSerializer.WriteTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.ScopeSpans_Span, ProtobufWireType.LEN);
            int spanLengthPosition = WritePosition;
            WritePosition += ReserveSizeForLength;

            WritePosition = ProtobufSerializer.WriteTagAndLength(Buffer, WritePosition, TraceIdSize, ProtobufOtlpTraceFieldNumberConstants.Span_Trace_Id, ProtobufWireType.LEN);
            WritePosition = WriteTraceId(Buffer, WritePosition, span.Info.TraceId);

            WritePosition = ProtobufSerializer.WriteTagAndLength(Buffer, WritePosition, SpanIdSize, ProtobufOtlpTraceFieldNumberConstants.Span_Span_Id, ProtobufWireType.LEN);
            WritePosition = WriteSpanId(Buffer, WritePosition, span.Info.SpanId);

            if (!string.IsNullOrEmpty(span.Info.TraceState))
            {
                WritePosition = ProtobufSerializer.WriteStringWithTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Trace_State, span.Info.TraceState);
            }

            if (span.Info.ParentSpanId != default)
            {
                WritePosition = ProtobufSerializer.WriteTagAndLength(Buffer, WritePosition, SpanIdSize, ProtobufOtlpTraceFieldNumberConstants.Span_Parent_Span_Id, ProtobufWireType.LEN);
                WritePosition = WriteSpanId(Buffer, WritePosition, span.Info.ParentSpanId);
            }

            WritePosition = WriteSpanFlags(Buffer, WritePosition, span.Info.TraceFlags, ProtobufOtlpTraceFieldNumberConstants.Span_Flags);

            WritePosition = ProtobufSerializer.WriteStringWithTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Name, span.Info.Name);

            if (span.Info.Kind.HasValue)
            {
                WritePosition = ProtobufSerializer.WriteEnumWithTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Kind, (int)span.Info.Kind + 1);
            }

            WritePosition = ProtobufSerializer.WriteFixed64WithTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_Start_Time_Unix_Nano, span.Info.StartTimestampUtc.ToUnixTimeNanoseconds());
            WritePosition = ProtobufSerializer.WriteFixed64WithTag(Buffer, WritePosition, ProtobufOtlpTraceFieldNumberConstants.Span_End_Time_Unix_Nano, span.Info.EndTimestampUtc.ToUnixTimeNanoseconds());

            WritePosition = WriteAttributes(Buffer, WritePosition, span.Attributes);
            WritePosition = WriteSpanEvents(Buffer, WritePosition, span.Events);
            WritePosition = WriteSpanLinks(Buffer, WritePosition, span.Links);
            WritePosition = WriteSpanStatus(Buffer, WritePosition, span.Info.StatusCode, span.Info.StatusDescription);

            ProtobufSerializer.WriteReservedLength(Buffer, spanLengthPosition, WritePosition - (spanLengthPosition + ReserveSizeForLength));
        }

        internal static void ProcessResourceAttribute(ref ProtobufOtlpTagWriter.OtlpTagWriterState otlpTagWriterState, KeyValuePair<string, object> attribute)
        {
            otlpTagWriterState.WritePosition = ProtobufSerializer.WriteTag(otlpTagWriterState.Buffer, otlpTagWriterState.WritePosition, ProtobufOtlpTraceFieldNumberConstants.Resource_Attributes, ProtobufWireType.LEN);
            int resourceAttributesLengthPosition = otlpTagWriterState.WritePosition;
            otlpTagWriterState.WritePosition += ReserveSizeForLength;

            ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, attribute.Key, attribute.Value);

            int resourceAttributesLength = otlpTagWriterState.WritePosition - (resourceAttributesLengthPosition + ReserveSizeForLength);
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
                otlpTagWriterState.WritePosition += ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, spanAttributesLengthPosition, otlpTagWriterState.WritePosition - (spanAttributesLengthPosition + ReserveSizeForLength));
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
                writePosition += ReserveSizeForLength; // Reserve 4 bytes for length

                writePosition = ProtobufSerializer.WriteStringWithTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Event_Name, evnt.Name);
                writePosition = ProtobufSerializer.WriteFixed64WithTag(buffer, writePosition, ProtobufOtlpTraceFieldNumberConstants.Event_Time_Unix_Nano, (ulong)evnt.TimestampUtc.ToUnixTimeNanoseconds());

                ref readonly var attributes = ref SpanEvent.GetAttributesReference(in evnt);

                writePosition = WriteEventAttributes(buffer, writePosition, attributes);
                ProtobufSerializer.WriteReservedLength(buffer, spanEventsLengthPosition, writePosition - (spanEventsLengthPosition + ReserveSizeForLength));
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
                otlpTagWriterState.WritePosition += ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, eventAttributesLengthPosition, otlpTagWriterState.WritePosition - (eventAttributesLengthPosition + ReserveSizeForLength));
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
                writePosition += ReserveSizeForLength;

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

                ProtobufSerializer.WriteReservedLength(buffer, spanLinksLengthPosition, writePosition - (spanLinksLengthPosition + ReserveSizeForLength));
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
                otlpTagWriterState.WritePosition += ReserveSizeForLength;

                ProtobufOtlpTagWriter.Instance.TryWriteTag(ref otlpTagWriterState, tag.Key, tag.Value);
                ProtobufSerializer.WriteReservedLength(buffer, linkAttributesLengthPosition, otlpTagWriterState.WritePosition - (linkAttributesLengthPosition + ReserveSizeForLength));
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
