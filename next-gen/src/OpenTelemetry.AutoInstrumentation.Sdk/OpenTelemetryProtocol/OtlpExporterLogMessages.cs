// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace OpenTelemetry.OpenTelemetryProtocol;

internal static partial class OtlpExporterLogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Telemetry sent successfully to '{Endpoint}' endpoint")]
    public static partial void OtlpTelemetryExportCompleted(
        this ILogger logger, Uri? endpoint);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Error status code '{StatusCode}' returned sending telemetry to '{Endpoint}' endpoint")]
    public static partial void OtlpTelemetryExportFailed(
        this ILogger logger, int statusCode, Uri? endpoint);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Exception thrown sending telemetry to '{Endpoint}' endpoint")]
    public static partial void OtlpTelemetryExportException(
        this ILogger logger, Exception exception, Uri? endpoint);
}
