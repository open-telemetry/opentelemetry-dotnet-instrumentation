// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.MassTransit.Consumers;

internal static partial class MassTransitLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received Text: {text}")]
    public static partial void LogReceivedText(this ILogger logger, string? text);
}
