// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using NLog;

namespace TestApplication.NLogBridge;

internal class NLogLogger : Microsoft.Extensions.Logging.ILogger
{
    private readonly NLog.ILogger _log;

    public NLogLogger(string categoryName)
    {
        _log = LogManager.GetLogger(categoryName);
    }

    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case Microsoft.Extensions.Logging.LogLevel.Information:
                _log.Info(formatter(state, exception));
                break;
            case Microsoft.Extensions.Logging.LogLevel.Error:
                _log.Error(exception, formatter(state, exception));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Critical => _log.IsFatalEnabled,
            Microsoft.Extensions.Logging.LogLevel.Debug or Microsoft.Extensions.Logging.LogLevel.Trace => _log.IsDebugEnabled,
            Microsoft.Extensions.Logging.LogLevel.Error => _log.IsErrorEnabled,
            Microsoft.Extensions.Logging.LogLevel.Information => _log.IsInfoEnabled,
            Microsoft.Extensions.Logging.LogLevel.Warning => _log.IsWarnEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}
