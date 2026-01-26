// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace TestApplication.Log4NetBridge;

internal static partial class Log4NetBridgeLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{hello}, {world} at {time:t}!")]
    public static partial void LogHelloWorld(this ILogger logger, string hello, string world, DateTime time);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception occurred")]
    public static partial void LogExceptionOccurred(this ILogger logger, Exception exception);
}
