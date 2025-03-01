// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace OpenTelemetry;

internal static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Telemetry export completed with '{Result}' using '{ExporterType}' exporter")]
    public static partial void TelemetryExportCompleted(
        this ILogger logger, bool result, string? exporterType);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Exception thrown exporting telemetry using '{ExporterType}' exporter")]
    public static partial void TelemetryExportException(
        this ILogger logger, Exception exception, string? exporterType);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Batch export processor using '{ExporterType}' exporter dropped '{NumberOfDroppedItems}' item(s) due to batch being full")]
    public static partial void BatchExporterDroppedItems(
        this ILogger logger, string? exporterType, long numberOfDroppedItems);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Telemetry collection completed with '{Result}' using '{ProducerType}' producer")]
    public static partial void MetricsCollectionCompleted(
        this ILogger logger, bool result, string? producerType);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Exception thrown collecting telemetry using '{ProducerType}' producer")]
    public static partial void MetricsCollectionException(
        this ILogger logger, Exception exception, string? producerType);
}
