// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Examples.Service;

internal static partial class LoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Success! Today is: {Date:MMMM dd, yyyy}")]
    public static partial void Success(this ILogger logger, DateTimeOffset date);
}
