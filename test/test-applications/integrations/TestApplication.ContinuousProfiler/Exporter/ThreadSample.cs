// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler;

internal class ThreadSample
{
    public Time? Timestamp { get; set; }

    public long SpanId { get; set; }

    public long TraceIdHigh { get; set; }

    public long TraceIdLow { get; set; }

    public int ManagedId { get; set; }

    public string? ThreadName { get; set; }

    public uint ThreadIndex { get; set; }

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
