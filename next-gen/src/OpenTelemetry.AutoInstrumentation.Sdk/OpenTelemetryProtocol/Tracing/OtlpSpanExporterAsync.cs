// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

using Google.Protobuf;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

using OtlpCollectorTrace = OpenTelemetry.Proto.Collector.Trace.V1;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpTrace = OpenTelemetry.Proto.Trace.V1;

namespace OpenTelemetry.OpenTelemetryProtocol.Tracing;

/// <summary>
/// OTLP span exporter.
/// </summary>
internal sealed class OtlpSpanExporterAsync : OtlpExporterAsync<OtlpCollectorTrace.ExportTraceServiceRequest, SpanBatchWriter>, ISpanExporterAsync
{
    private readonly OtlpSpanWriter _Writer = new();

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
    }

    /// <inheritdoc/>
    public override Task<bool> ExportAsync<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
    {
        var writer = _Writer;

        try
        {
            if (!batch.WriteTo(writer))
            {
                return Task.FromResult(false);
            }

            var sendTask = SendAsync(writer.Request, cancellationToken);

            Debug.Assert(sendTask.IsCompleted);

            return sendTask;
        }
        finally
        {
            writer.Reset();
        }
    }

    private sealed class OtlpSpanWriter : SpanBatchWriter
    {
        private static void AddSpanLink(OtlpTrace.Span otlpSpan, in SpanLink spanLink)
        {
            byte[] traceIdBytes = new byte[16];
            byte[] spanIdBytes = new byte[8];

            ref readonly var spanContext = ref SpanLink.GetSpanContextReference(in spanLink);

            spanContext.TraceId.CopyTo(traceIdBytes);
            spanContext.SpanId.CopyTo(spanIdBytes);

            var otlpLink = new OtlpTrace.Span.Types.Link()
            {
                TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes),
                SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes),
                Flags = (uint)spanContext.TraceFlags,
                TraceState = spanContext.TraceState ?? string.Empty,
            };

            ref readonly var attributes = ref SpanLink.GetAttributesReference(in spanLink);

            OtlpCommon.OtlpCommonExtensions.AddRange(otlpLink.Attributes, attributes);

            otlpSpan.Links.Add(otlpLink);
        }

        private static void AddSpanEvent(OtlpTrace.Span otlpSpan, in SpanEvent spanEvent)
        {
            var otlpEvent = new OtlpTrace.Span.Types.Event()
            {
                Name = spanEvent.Name,
                TimeUnixNano = spanEvent.TimestampUtc.ToUnixTimeNanoseconds()
            };

            ref readonly var attributes = ref SpanEvent.GetAttributesReference(in spanEvent);

            OtlpCommon.OtlpCommonExtensions.AddRange(otlpEvent.Attributes, attributes);

            otlpSpan.Events.Add(otlpEvent);
        }

        private OtlpTrace.ResourceSpans? _ResourceSpans;
        private OtlpTrace.ScopeSpans? _ScopeSpans;

        public OtlpSpanWriter()
        {
            Reset();
        }

        public OtlpCollectorTrace.ExportTraceServiceRequest Request { get; private set; }

        [MemberNotNull(nameof(Request))]
        public void Reset()
        {
            Request = new();
            _ResourceSpans = null;
            _ScopeSpans = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(_ResourceSpans == null);

            _ResourceSpans = new()
            {
                Resource = new()
            };

            foreach (var resourceAttribute in resource.Attributes)
            {
                _ResourceSpans.Resource.Attributes.Add(
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
            Debug.Assert(_ResourceSpans != null);

            Request.ResourceSpans.Add(_ResourceSpans);
            _ResourceSpans = null;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);
            Debug.Assert(_ResourceSpans != null);
            Debug.Assert(_ScopeSpans == null);

            _ScopeSpans = _ResourceSpans.ScopeSpans.FirstOrDefault(s => s.Scope.Name == instrumentationScope.Name);
            if (_ScopeSpans == null)
            {
                _ScopeSpans = new()
                {
                    Scope = new()
                    {
                        Name = instrumentationScope.Name,
                    }
                };

                if (!string.IsNullOrEmpty(instrumentationScope.Version))
                {
                    _ScopeSpans.Scope.Version = instrumentationScope.Version;
                }

                _ResourceSpans.ScopeSpans.Add(_ScopeSpans);
            }
        }

        public override void EndInstrumentationScope()
        {
            Debug.Assert(_ScopeSpans != null);

            _ScopeSpans = null;
        }

        public override void WriteSpan(in Span span)
        {
            Debug.Assert(_ScopeSpans != null);

            byte[] traceIdBytes = new byte[16];
            byte[] spanIdBytes = new byte[8];

            span.Info.TraceId.CopyTo(traceIdBytes);
            span.Info.SpanId.CopyTo(spanIdBytes);

            var parentSpanIdString = ByteString.Empty;
            if (span.Info.ParentSpanId != default)
            {
                byte[] parentSpanIdBytes = new byte[8];
                span.Info.ParentSpanId.CopyTo(parentSpanIdBytes);
                parentSpanIdString = UnsafeByteOperations.UnsafeWrap(parentSpanIdBytes);
            }

            var otlpSpan = new OtlpTrace.Span()
            {
                Name = span.Info.Name,

                TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes),
                SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes),
                Flags = (uint)span.Info.TraceFlags,
                ParentSpanId = parentSpanIdString,
                TraceState = span.Info.TraceState ?? string.Empty,

                StartTimeUnixNano = span.Info.StartTimestampUtc.ToUnixTimeNanoseconds(),
                EndTimeUnixNano = span.Info.EndTimestampUtc.ToUnixTimeNanoseconds(),
            };

            if (span.Info.Kind.HasValue)
            {
                // There is an offset of 1 on the OTLP enum.
                otlpSpan.Kind = (OtlpTrace.Span.Types.SpanKind)(span.Info.Kind + 1);
            }

            switch (span.Info.StatusCode)
            {
                case ActivityStatusCode.Ok:
                    otlpSpan.Status = new()
                    {
                        Code = OtlpTrace.Status.Types.StatusCode.Ok
                    };
                    break;
                case ActivityStatusCode.Error:
                    otlpSpan.Status = new()
                    {
                        Code = OtlpTrace.Status.Types.StatusCode.Error
                    };

                    if (!string.IsNullOrEmpty(span.Info.StatusDescription))
                    {
                        otlpSpan.Status.Message = span.Info.StatusDescription;
                    }

                    break;
            }

            OtlpCommon.OtlpCommonExtensions.AddRange(otlpSpan.Attributes, span.Attributes);

            foreach (ref readonly var spanLink in span.Links)
            {
                AddSpanLink(otlpSpan, in spanLink);
            }

            foreach (ref readonly var spanEvent in span.Events)
            {
                AddSpanEvent(otlpSpan, in spanEvent);
            }

            _ScopeSpans.Spans.Add(otlpSpan);
        }
    }
}
