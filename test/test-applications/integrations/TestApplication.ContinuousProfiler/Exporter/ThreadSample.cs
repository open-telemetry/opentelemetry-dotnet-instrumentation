// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

internal class ThreadSample
{
    public ThreadSample(long timestampMilliseconds, long traceIdHigh, long traceIdLow, long spanId, string? threadName, uint threadIndex = default)
    {
        TimestampNanoseconds = (ulong)timestampMilliseconds * 1_000_000u;
        TraceIdHigh = traceIdHigh;
        TraceIdLow = traceIdLow;
        SpanId = spanId;
        ThreadName = threadName;
        ThreadIndex = threadIndex;
    }

    public ulong TimestampNanoseconds { get; }

    public long SpanId { get; }

    public long TraceIdHigh { get; }

    public long TraceIdLow { get; }

    public string? ThreadName { get; }

    public uint ThreadIndex { get; }

    public IList<string> Frames { get; } = new List<string>();
}
