// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if SERILOG_HOSTING
using Microsoft.Extensions.Logging;

namespace TestApplication.SerilogBridge;

internal static partial class SerilogBridgeLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Logged through ILogger {Source}")]
    public static partial void LogThroughILogger(this ILogger logger, string source);
}
#endif
