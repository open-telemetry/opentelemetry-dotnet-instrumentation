// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OtlpCollector = OpenTelemetry.Proto.Collector.Metrics.V1;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests;

internal sealed class CollectedMeasurement
{
    public string InstrumentName { get; set; } = string.Empty;

    public object Value { get; set; } = 0; // Changed to object to support different types

    public KeyValuePair<string, object?>[] Tags { get; set; } = Array.Empty<KeyValuePair<string, object?>>();

    public OtlpCollector.ExportMetricsServiceRequest Request { get; set; } = new();

    public DateTime Timestamp { get; set; }

    // Helper methods for type-safe value access
    public T GetValue<T>()
        where T : struct
        => (T)Value;

    public int GetIntValue() => GetValue<int>();

    public long GetLongValue() => GetValue<long>();

    public double GetDoubleValue() => GetValue<double>();

    public float GetFloatValue() => GetValue<float>();
}
