// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal class InternalLogger : IOtelLogger
{
    private static readonly object[] NoPropertyValues = Array.Empty<object>();

    private ISink _sink;

    internal InternalLogger(ISink sink, LogLevel logLevel)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        Level = logLevel;
    }

    public LogLevel Level { get; }

    public bool IsEnabled(LogLevel level)
    {
        return level <= Level;
    }

    public void Debug(string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Debug, exception: null, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Debug<T>(string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Debug, exception: null, messageTemplate, property, writeToEventLog);

    public void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Debug, exception: null, messageTemplate, property0, property1, writeToEventLog);

    public void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Debug, exception: null, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Debug(string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Debug, exception: null, messageTemplate, args, writeToEventLog);

    public void Debug(Exception exception, string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Debug, exception, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Debug<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Debug, exception, messageTemplate, property, writeToEventLog);

    public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Debug, exception, messageTemplate, property0, property1, writeToEventLog);

    public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Debug, exception, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Debug(Exception exception, string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Debug, exception, messageTemplate, args, writeToEventLog);

    public void Information(string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Information, exception: null, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Information<T>(string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Information, exception: null, messageTemplate, property, writeToEventLog);

    public void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Information, exception: null, messageTemplate, property0, property1, writeToEventLog);

    public void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Information, exception: null, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Information(string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Information, exception: null, messageTemplate, args, writeToEventLog);

    public void Information(Exception exception, string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Information, exception, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Information<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Information, exception, messageTemplate, property, writeToEventLog);

    public void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Information, exception, messageTemplate, property0, property1, writeToEventLog);

    public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Information, exception, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Information(Exception exception, string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Information, exception, messageTemplate, args, writeToEventLog);

    public void Warning(string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Warning, exception: null, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Warning<T>(string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Warning, exception: null, messageTemplate, property, writeToEventLog);

    public void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Warning, exception: null, messageTemplate, property0, property1, writeToEventLog);

    public void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Warning, exception: null, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Warning(string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Warning, exception: null, messageTemplate, args, writeToEventLog);

    public void Warning(Exception exception, string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Warning, exception, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Warning<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Warning, exception, messageTemplate, property, writeToEventLog);

    public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Warning, exception, messageTemplate, property0, property1, writeToEventLog);

    public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Warning, exception, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Warning(Exception exception, string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Warning, exception, messageTemplate, args, writeToEventLog);

    public void Error(string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Error, exception: null, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Error<T>(string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Error, exception: null, messageTemplate, property, writeToEventLog);

    public void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Error, exception: null, messageTemplate, property0, property1, writeToEventLog);

    public void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Error, exception: null, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Error(string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Error, exception: null, messageTemplate, args, writeToEventLog);

    public void Error(Exception exception, string messageTemplate, bool writeToEventLog)
        => Write(LogLevel.Error, exception, messageTemplate, NoPropertyValues, writeToEventLog);

    public void Error<T>(Exception exception, string messageTemplate, T property, bool writeToEventLog)
        => Write(LogLevel.Error, exception, messageTemplate, property, writeToEventLog);

    public void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
        => Write(LogLevel.Error, exception, messageTemplate, property0, property1, writeToEventLog);

    public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
        => Write(LogLevel.Error, exception, messageTemplate, property0, property1, property2, writeToEventLog);

    public void Error(Exception exception, string messageTemplate, object[] args, bool writeToEventLog)
        => Write(LogLevel.Error, exception, messageTemplate, args, writeToEventLog);

    public void Close()
    {
        // Replace with Noop sink, so that attempts to write after logger is closed
        // don't try to write to disposed sink.

        var oldSink = Interlocked.Exchange(ref _sink, NoopSink.Instance);
        if (oldSink is IDisposable disposableSink)
        {
            disposableSink.Dispose();
        }
    }

    private static void WriteEventSourceLog(LogLevel level, string message)
    {
        switch (level)
        {
            case LogLevel.Information:
                AutoInstrumentationEventSource.Log.Information(message);
                break;
            case LogLevel.Debug:
                AutoInstrumentationEventSource.Log.Verbose(message);
                break;
            case LogLevel.Warning:
                AutoInstrumentationEventSource.Log.Warning(message);
                break;
            case LogLevel.Error:
                AutoInstrumentationEventSource.Log.Error(message);
                break;
        }
    }

    private void Write<T>(LogLevel level, Exception? exception, string messageTemplate, T property, bool writeToEventLog)
    {
        // Avoid boxing + array allocation if disabled
        if (IsEnabled(level))
        {
            WriteImpl(level, exception, messageTemplate, new object?[] { property }, writeToEventLog);
        }
    }

    private void Write<T0, T1>(LogLevel level, Exception? exception, string messageTemplate, T0 property0, T1 property1, bool writeToEventLog)
    {
        // Avoid boxing + array allocation if disabled
        if (IsEnabled(level))
        {
            WriteImpl(level, exception, messageTemplate, new object?[] { property0, property1 }, writeToEventLog);
        }
    }

    private void Write<T0, T1, T2>(LogLevel level, Exception? exception, string messageTemplate, T0 property0, T1 property1, T2 property2, bool writeToEventLog)
    {
        // Avoid boxing + array allocation if disabled
        if (IsEnabled(level))
        {
            WriteImpl(level, exception, messageTemplate, new object?[] { property0, property1, property2 }, writeToEventLog);
        }
    }

    private void Write(LogLevel level, Exception? exception, string messageTemplate, object[] args, bool writeToEventLog)
    {
        if (IsEnabled(level))
        {
            // logging is not disabled
            WriteImpl(level, exception, messageTemplate, args, writeToEventLog);
        }
    }

    private void WriteImpl(LogLevel level, Exception? exception, string messageTemplate, object?[] args, bool writeToEventLog)
    {
        try
        {
            // Use template if no arguments provided, otherwise format the template with provided arguments.
            var rawMessage = args == NoPropertyValues ? messageTemplate : string.Format(CultureInfo.InvariantCulture, messageTemplate, args);
            if (exception != null)
            {
                rawMessage += $"{Environment.NewLine}Exception: {exception.Message}{Environment.NewLine}{exception}";
            }

            _sink.Write($"[{DateTime.UtcNow:O}] [{level}] {rawMessage} {Environment.NewLine}");
            if (writeToEventLog)
            {
                WriteEventSourceLog(level, rawMessage);
            }
        }
        catch (Exception)
        {
            try
            {
                var ex = exception is null ? string.Empty : $"; {exception}";
                var properties = args.Length == 0
                    ? string.Empty
                    : "; " + string.Join(", ", args);
                Console.Error.WriteLine($"{messageTemplate}{properties}{ex}");
            }
            catch
            {
                // ignore
            }
        }
    }
}
