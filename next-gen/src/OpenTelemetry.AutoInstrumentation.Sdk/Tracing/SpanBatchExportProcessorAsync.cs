// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Tracing;

internal sealed class SpanBatchExportProcessorAsync : BatchExportProcessorAsync<BufferedSpan, SpanBatchWriter, BufferedSpanBatch>, ISpanProcessor
{
    public SpanBatchExportProcessorAsync(
        ILogger<SpanBatchExportProcessorAsync> logger,
        Resource resource,
        ISpanExporterAsync exporter,
        BatchExportProcessorOptions options)
        : base(logger, resource, exporter, options)
    {
    }

    public void ProcessEndedSpan(in Span span)
    {
        var bufferedItem = new BufferedSpan(in span);

        AddItemToBatch(bufferedItem);
    }

    protected override void CreateBatch(
        BufferedTelemetryBatch<BufferedSpan> bufferedBatch,
        out BufferedSpanBatch batch)
    {
        batch = new(bufferedBatch);
    }
}
