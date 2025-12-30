// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Logs.Controllers;

internal static partial class TestControllerLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Information from Test App.")]
    public static partial void LogInformationFromTestApp(this ILogger logger);
}
