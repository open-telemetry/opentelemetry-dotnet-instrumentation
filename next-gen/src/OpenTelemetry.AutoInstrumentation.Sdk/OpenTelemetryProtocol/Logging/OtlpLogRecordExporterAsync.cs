// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

using Google.Protobuf;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logging;
using OpenTelemetry.Resources;

using OtlpCollectorLogs = OpenTelemetry.Proto.Collector.Logs.V1;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;

namespace OpenTelemetry.OpenTelemetryProtocol.Logging;

/// <summary>
/// OTLP log record exporter.
/// </summary>
internal sealed class OtlpLogRecordExporterAsync : OtlpExporterAsync<OtlpCollectorLogs.ExportLogsServiceRequest, LogRecordBatchWriter>, ILogRecordExporterAsync
{
    [ThreadStatic]
    private static OtlpLogRecordWriter? s_Writer;

    private readonly ILogger<OtlpLogRecordExporterAsync> _Logger;

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
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override Task<bool> ExportAsync<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
    {
        var writer = s_Writer ??= new();

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

    private sealed class OtlpLogRecordWriter : LogRecordBatchWriter
    {
        private OtlpLogs.ResourceLogs? _ResourceLogs;
        private OtlpLogs.ScopeLogs? _ScopeLogs;

        public OtlpLogRecordWriter()
        {
            Reset();
        }

        public OtlpCollectorLogs.ExportLogsServiceRequest Request { get; private set; }

        [MemberNotNull(nameof(Request))]
        public void Reset()
        {
            Request = new();
            _ResourceLogs = null;
            _ScopeLogs = null;
        }

        public override void BeginBatch(Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(_ResourceLogs == null);

            _ResourceLogs = new();

            _ResourceLogs.Resource = new();

            foreach (var resourceAttribute in resource.Attributes)
            {
                _ResourceLogs.Resource.Attributes.Add(
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
            Debug.Assert(_ResourceLogs != null);

            Request.ResourceLogs.Add(_ResourceLogs);
            _ResourceLogs = null;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            Debug.Assert(instrumentationScope != null);
            Debug.Assert(_ResourceLogs != null);
            Debug.Assert(_ScopeLogs == null);

            _ScopeLogs = _ResourceLogs.ScopeLogs.FirstOrDefault(s => s.Scope.Name == instrumentationScope.Name);
            if (_ScopeLogs == null)
            {
                _ScopeLogs = new()
                {
                    Scope = new()
                    {
                        Name = instrumentationScope.Name,
                    }
                };

                if (!string.IsNullOrEmpty(instrumentationScope.Version))
                {
                    _ScopeLogs.Scope.Version = instrumentationScope.Version;
                }

                _ResourceLogs.ScopeLogs.Add(_ScopeLogs);
            }
        }

        public override void EndInstrumentationScope()
        {
            Debug.Assert(_ScopeLogs != null);

            _ScopeLogs = null;
        }

        public override void WriteLogRecord(in LogRecord logRecord)
        {
            Debug.Assert(_ScopeLogs != null);

            var otlpLogRecord = new OtlpLogs.LogRecord
            {
                TimeUnixNano = logRecord.Info.TimestampUtc.ToUnixTimeNanoseconds(),
                ObservedTimeUnixNano = logRecord.Info.ObservedTimestampUtc.ToUnixTimeNanoseconds(),
                SeverityNumber = (OtlpLogs.SeverityNumber)(int)logRecord.Info.Severity,
            };

            if (!string.IsNullOrEmpty(logRecord.Info.Body))
            {
                otlpLogRecord.Body = new OtlpCommon.AnyValue { StringValue = logRecord.Info.Body };
            }

            if (!string.IsNullOrEmpty(logRecord.Info.SeverityText))
            {
                otlpLogRecord.SeverityText = logRecord.Info.SeverityText;
            }

            ref readonly var spanContext = ref logRecord.SpanContext;

            if (spanContext.TraceId != default && spanContext.SpanId != default)
            {
                byte[] traceIdBytes = new byte[16];
                byte[] spanIdBytes = new byte[8];

                spanContext.TraceId.CopyTo(traceIdBytes);
                spanContext.SpanId.CopyTo(spanIdBytes);

                otlpLogRecord.TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes);
                otlpLogRecord.SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes);
                otlpLogRecord.Flags = (uint)spanContext.TraceFlags;
            }

            OtlpCommon.OtlpCommonExtensions.AddRange(otlpLogRecord.Attributes, logRecord.Attributes);

            _ScopeLogs.LogRecords.Add(otlpLogRecord);
        }
    }
}
