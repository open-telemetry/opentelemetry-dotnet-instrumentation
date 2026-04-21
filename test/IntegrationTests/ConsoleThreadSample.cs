// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by deserializer.
internal sealed class ConsoleThreadSample
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by deserializer.
{
    public long TimestampNanoseconds { get; set; }

    public long SpanId { get; set; }

    public long TraceIdHigh { get; set; }

    public long TraceIdLow { get; set; }

    public string? ThreadName { get; set; }

    public string Source { get; set; } = string.Empty;

    public bool SelectedForFrequentSampling { get; set; }
}
