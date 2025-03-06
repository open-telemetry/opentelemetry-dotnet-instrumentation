// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

internal readonly ref struct BufferedSpanBatch : IBatch<SpanBatchWriter>
{
    private static void WriteItemCallback(
        SpanBatchWriter writer,
        BufferedSpan bufferedSpan)
    {
        bufferedSpan.ToSpan(out var span);

        writer.WriteSpan(in span);
    }

    private readonly BufferedTelemetryBatch<BufferedSpan> _BufferedBatch;

    public BufferedSpanBatch(
        BufferedTelemetryBatch<BufferedSpan> bufferedBatch)
    {
        Debug.Assert(bufferedBatch != null);

        _BufferedBatch = bufferedBatch;
    }

    public bool WriteTo(SpanBatchWriter writer)
        => _BufferedBatch.WriteTo(writer, WriteItemCallback);
}
