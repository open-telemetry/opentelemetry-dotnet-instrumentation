// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;

using SdkMetrics = OpenTelemetry.Metrics;

namespace OpenTelemetry.Proto.Metrics.V1;

internal static class OtlpMetricsExtensions
{
    public static void AddExemplar(
        this RepeatedField<Exemplar> exemplars,
        SdkMetrics.Metric metric,
        in SdkMetrics.Exemplar exemplar)
    {
        Debug.Assert(exemplars != null);
        Debug.Assert(metric != null);

        var otlpExemplar = new Exemplar()
        {
            TimeUnixNano = exemplar.TimestampUtc.ToUnixTimeNanoseconds()
        };

        if (exemplar.TraceId != default && exemplar.SpanId != default)
        {
            byte[] traceIdBytes = new byte[16];
            byte[] spanIdBytes = new byte[8];

            exemplar.TraceId.CopyTo(traceIdBytes);
            exemplar.SpanId.CopyTo(spanIdBytes);

            otlpExemplar.TraceId = UnsafeByteOperations.UnsafeWrap(traceIdBytes);
            otlpExemplar.SpanId = UnsafeByteOperations.UnsafeWrap(spanIdBytes);
        }

        if (!metric.IsFloatingPoint)
        {
            otlpExemplar.AsInt = exemplar.ValueAsLong;
        }
        else
        {
            otlpExemplar.AsDouble = exemplar.ValueAsDouble;
        }

        ref readonly TagList filteredAttributes = ref SdkMetrics.Exemplar.GetFilteredAttributesReference(in exemplar);

        otlpExemplar.FilteredAttributes.AddRange(in filteredAttributes);

        exemplars.Add(otlpExemplar);
    }
}
