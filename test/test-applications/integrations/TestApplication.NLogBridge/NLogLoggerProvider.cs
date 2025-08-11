// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

namespace TestApplication.NLogBridge;

public class NLogLoggerProvider : ILoggerProvider
{
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new NLogLogger(categoryName);
    }

    public void Dispose()
    {
    }
}
