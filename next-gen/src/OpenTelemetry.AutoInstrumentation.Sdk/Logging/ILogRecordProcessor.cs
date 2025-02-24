// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Logging;

/// <summary>
/// Describes the contract for a log record processor.
/// </summary>
public interface ILogRecordProcessor : IProcessor
{
    /// <summary>
    /// Process an emitted log record.
    /// </summary>
    /// <param name="logRecord"><see cref="LogRecord"/>.</param>
    void ProcessEmittedLogRecord(in LogRecord logRecord);
}
