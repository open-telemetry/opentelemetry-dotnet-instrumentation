// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Logging;

internal sealed class LogRecordBatchExportProcessorAsync : BatchExportProcessorAsync<BufferedLogRecord, LogRecordBatchWriter, BufferedLogRecordBatch>, ILogRecordProcessor
{
    public LogRecordBatchExportProcessorAsync(
        ILogger<LogRecordBatchExportProcessorAsync> logger,
        Resource resource,
        ILogRecordExporterAsync exporter,
        BatchExportProcessorOptions options)
        : base(logger, resource, exporter, options)
    {
    }

    public void ProcessEmittedLogRecord(in LogRecord logRecord)
    {
        var bufferedItem = new BufferedLogRecord(in logRecord);

        AddItemToBatch(bufferedItem);
    }

    protected override void CreateBatch(
        BufferedTelemetryBatch<BufferedLogRecord> bufferedBatch,
        out BufferedLogRecordBatch batch)
    {
        batch = new(bufferedBatch);
    }
}
