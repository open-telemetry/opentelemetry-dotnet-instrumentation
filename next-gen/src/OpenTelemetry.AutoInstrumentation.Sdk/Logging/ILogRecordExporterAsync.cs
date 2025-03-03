// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Logging;

/// <summary>
/// Describes the contract for exporting batches of log records asynchronously.
/// </summary>
public interface ILogRecordExporterAsync : IExporterAsync<LogRecordBatchWriter>
{
}
