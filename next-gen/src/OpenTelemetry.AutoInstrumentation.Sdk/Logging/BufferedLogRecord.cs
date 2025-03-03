// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.Logging;

internal sealed class BufferedLogRecord : IBufferedTelemetry<BufferedLogRecord>
{
    private readonly ActivityContext _SpanContext;
    private readonly LogRecordInfo _Info;
    private readonly List<KeyValuePair<string, object?>>? _Attributes;

    public BufferedLogRecord(in LogRecord logRecord)
    {
        _SpanContext = logRecord.SpanContext;
        _Info = logRecord.Info;

        _Attributes = [.. logRecord.Attributes];
    }

    public InstrumentationScope Scope => _Info.Scope;

    public BufferedLogRecord? Next { get; set; }

    public ref readonly ActivityContext SpanContext => ref _SpanContext;

    public ref readonly LogRecordInfo Info => ref _Info;

    public void ToLogRecord(out LogRecord logRecord)
    {
        logRecord = new LogRecord(
            in _SpanContext,
            in _Info)
        {
            Attributes = CollectionsMarshal.AsSpan(_Attributes)
        };
    }
}
