// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Logging;

internal readonly ref struct BufferedLogRecordBatch : IBatch<LogRecordBatchWriter>
{
    private static void WriteItemCallback(
        LogRecordBatchWriter writer,
        BufferedLogRecord bufferedLogRecord)
    {
        bufferedLogRecord.ToLogRecord(out LogRecord logRecord);

        writer.WriteLogRecord(in logRecord);
    }

    private readonly BufferedTelemetryBatch<BufferedLogRecord> _BufferedBatch;

    public BufferedLogRecordBatch(
        BufferedTelemetryBatch<BufferedLogRecord> bufferedBatch)
    {
        Debug.Assert(bufferedBatch != null);

        _BufferedBatch = bufferedBatch;
    }

    public bool WriteTo(LogRecordBatchWriter writer)
        => _BufferedBatch.WriteTo(writer, WriteItemCallback);
}
