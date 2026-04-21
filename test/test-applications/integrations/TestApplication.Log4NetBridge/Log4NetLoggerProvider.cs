// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace TestApplication.Log4NetBridge;

internal sealed class Log4NetLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new Log4NetLogger(categoryName);
    }

    public void Dispose()
    {
    }
}
