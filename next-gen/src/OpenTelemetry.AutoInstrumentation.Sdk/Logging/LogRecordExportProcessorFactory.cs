// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Logging;

/// <summary>
/// Implements a factory for creating log record export processors.
/// </summary>
public static class LogRecordExportProcessorFactory
{
    /// <summary>
    /// Create an asynchronous batch log record export processor.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    /// <param name="resource"><see cref="Resource"/>.</param>
    /// <param name="exporter"><see cref="ILogRecordExporterAsync"/>.</param>
    /// <param name="options"><see cref="BatchExportProcessorOptions"/>.</param>
    /// <returns><see cref="ILogRecordProcessor"/>.</returns>
    public static ILogRecordProcessor CreateBatchExportProcessorAsync(
        ILoggerFactory loggerFactory,
        Resource resource,
        ILogRecordExporterAsync exporter,
        BatchExportProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new LogRecordBatchExportProcessorAsync(
            loggerFactory.CreateLogger<LogRecordBatchExportProcessorAsync>(),
            resource,
            exporter,
            options);
    }
}
