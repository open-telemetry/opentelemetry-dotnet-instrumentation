// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Tracing;

/// <summary>
/// Implements a factory for creating span export processors.
/// </summary>
public static class SpanExportProcessorFactory
{
    /// <summary>
    /// Create an asynchronous batch span export processor.
    /// </summary>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/>.</param>
    /// <param name="resource"><see cref="Resource"/>.</param>
    /// <param name="exporter"><see cref="ISpanExporterAsync"/>.</param>
    /// <param name="options"><see cref="BatchExportProcessorOptions"/>.</param>
    /// <returns><see cref="ISpanProcessor"/>.</returns>
    public static ISpanProcessor CreateBatchExportProcessorAsync(
        ILoggerFactory loggerFactory,
        Resource resource,
        ISpanExporterAsync exporter,
        BatchExportProcessorOptions options)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return new SpanBatchExportProcessorAsync(
            loggerFactory.CreateLogger<SpanBatchExportProcessorAsync>(),
            resource,
            exporter,
            options);
    }
}
