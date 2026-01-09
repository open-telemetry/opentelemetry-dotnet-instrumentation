// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace TestApplication.Smoke;

internal static partial class SmokeLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Example log message")]
    public static partial void LogExampleMessage(this ILogger logger);
}
