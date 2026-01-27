// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using log4net;
using Microsoft.Extensions.Logging;

namespace TestApplication.Log4NetBridge;

internal sealed class Log4NetLogger : ILogger
{
    private readonly ILog _log;

    public Log4NetLogger(string categoryName)
    {
        _log = LogManager.GetLogger(categoryName);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Information:
                _log.Info(formatter(state, exception));
                break;
            case LogLevel.Error:
                _log.Error(formatter(state, exception), exception);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Critical => _log.IsFatalEnabled,
            LogLevel.Debug or LogLevel.Trace => _log.IsDebugEnabled,
            LogLevel.Error => _log.IsErrorEnabled,
            LogLevel.Information => _log.IsInfoEnabled,
            LogLevel.Warning => _log.IsWarnEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
        };
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}
