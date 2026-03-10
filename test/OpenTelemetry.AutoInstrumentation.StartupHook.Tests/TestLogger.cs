// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Test implementation.
/// </summary>
internal sealed class TestLogger : IOtelLogger
{
    public List<LogRecord> LogRecords { get; } = new List<LogRecord>();

    public LogLevel Level => default;

    public bool IsEnabled(LogLevel level)
    {
        return true;
    }

    public void Debug(string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Debug<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Debug(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Debug(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Debug<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Debug(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Information(string messageTemplate, bool writeToEventLog = true)
    {
        var logRecord = new LogRecord(LogLevel.Information, messageTemplate);
        LogRecords.Add(logRecord);
    }

    public void Information<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Information(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Information(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Information<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Information(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Warning(string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Warning<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Warning(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Warning(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Warning<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Warning(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Error(string messageTemplate, bool writeToEventLog = true)
    {
        var logRecord = new LogRecord(LogLevel.Error, messageTemplate);
        LogRecords.Add(logRecord);
    }

    public void Error<T>(string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Error(string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Error(Exception exception, string messageTemplate, bool writeToEventLog = true)
    {
    }

    public void Error<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog = true)
    {
    }

    public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog = true)
    {
    }

    public void Error(Exception exception, string messageTemplate, object[] args, bool writeToEventLog = true)
    {
    }

    public void Close()
    {
    }

    internal sealed class LogRecord
    {
        public LogRecord(LogLevel logLevel, string message, Exception? exception = null)
        {
            LogLevel = logLevel;
            Message = message;
            Exception = exception;
        }

        public LogLevel LogLevel { get; }

        public string Message { get; }

        public Exception? Exception { get; }
    }
}
