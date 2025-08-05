// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry.Logging;

/// <summary>
/// Describes a <see cref="IBatchWriter"/> for writing batches of log records.
/// </summary>
public abstract class LogRecordBatchWriter : IBatchWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogRecordBatchWriter"/> class.
    /// </summary>
    protected LogRecordBatchWriter()
    {
    }

    /// <inheritdoc/>
    public virtual void BeginBatch(
        Resource resource)
    {
    }

    /// <inheritdoc/>
    public virtual void EndBatch()
    {
    }

    /// <inheritdoc/>
    public virtual void BeginInstrumentationScope(
        InstrumentationScope instrumentationScope)
    {
    }

    /// <inheritdoc/>
    public virtual void EndInstrumentationScope()
    {
    }

    /// <summary>
    /// Write a log record.
    /// </summary>
    /// <param name="logRecord"><see cref="LogRecord"/>.</param>
    public virtual void WriteLogRecord(in LogRecord logRecord)
    {
    }
}
