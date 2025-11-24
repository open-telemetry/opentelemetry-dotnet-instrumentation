// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests;

internal class ConsoleThreadSample
{
    public long TimestampNanoseconds { get; set; }

    public long SpanId { get; set; }

    public long TraceIdHigh { get; set; }

    public long TraceIdLow { get; set; }

    public string? ThreadName { get; set; }

    public bool SelectedForFrequentSampling { get; set; }
}
