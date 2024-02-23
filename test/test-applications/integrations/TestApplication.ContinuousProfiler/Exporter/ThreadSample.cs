// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

internal class ThreadSample
{
    public ThreadSample(Time timestamp, long traceIdHigh, long traceIdLow, long spanId, string? threadName, uint threadIndex = default)
    {
        Timestamp = timestamp;
        TraceIdHigh = traceIdHigh;
        TraceIdLow = traceIdLow;
        SpanId = spanId;
        ThreadName = threadName;
        ThreadIndex = threadIndex;
    }

    public Time Timestamp { get; }

    public long SpanId { get; }

    public long TraceIdHigh { get; }

    public long TraceIdLow { get; }

    public string? ThreadName { get; }

    public uint ThreadIndex { get; }

    public IList<string> Frames { get; } = new List<string>();

    internal class Time
    {
        public Time(long milliseconds)
        {
            Milliseconds = milliseconds;
            Nanoseconds = (ulong)milliseconds * 1_000_000u;
        }

        public ulong Nanoseconds { get; }

        public long Milliseconds { get; }
    }
}
