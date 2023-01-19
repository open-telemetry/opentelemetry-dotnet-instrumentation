// <copyright file="CustomLogger.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal class CustomLogger : IOtelLogger
{
    private static readonly object[] NoPropertyValues = Array.Empty<object>();

    private readonly ISink _sink;

    internal CustomLogger(ISink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public bool IsEnabled(LogLevel level)
    {
        // Log all at the moment
        return true;
    }

    public void Debug(string messageTemplate)
        => Write(LogLevel.Debug, exception: null, messageTemplate, NoPropertyValues);

    public void Debug<T>(string messageTemplate, T property)
        => Write(LogLevel.Debug, exception: null, messageTemplate, property);

    public void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Debug, exception: null, messageTemplate, property0, property1);

    public void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Debug, exception: null, messageTemplate, property0, property1, property2);

    public void Debug(string messageTemplate, object[] args)
        => Write(LogLevel.Debug, exception: null, messageTemplate, args);

    public void Debug(Exception exception, string messageTemplate)
        => Write(LogLevel.Debug, exception, messageTemplate, NoPropertyValues);

    public void Debug<T>(Exception exception, string messageTemplate, T property)
        => Write(LogLevel.Debug, exception, messageTemplate, property);

    public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Debug, exception, messageTemplate, property0, property1);

    public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Debug, exception, messageTemplate, property0, property1, property2);

    public void Debug(Exception exception, string messageTemplate, object[] args)
        => Write(LogLevel.Debug, exception, messageTemplate, args);

    public void Information(string messageTemplate)
        => Write(LogLevel.Information, exception: null, messageTemplate, NoPropertyValues);

    public void Information<T>(string messageTemplate, T property)
        => Write(LogLevel.Information, exception: null, messageTemplate, property);

    public void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Information, exception: null, messageTemplate, property0, property1);

    public void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Information, exception: null, messageTemplate, property0, property1, property2);

    public void Information(string messageTemplate, object[] args)
        => Write(LogLevel.Information, exception: null, messageTemplate, args);

    public void Information(Exception exception, string messageTemplate)
        => Write(LogLevel.Information, exception, messageTemplate, NoPropertyValues);

    public void Information<T>(Exception exception, string messageTemplate, T property)
        => Write(LogLevel.Information, exception, messageTemplate, property);

    public void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Information, exception, messageTemplate, property0, property1);

    public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Information, exception, messageTemplate, property0, property1, property2);

    public void Information(Exception exception, string messageTemplate, object[] args)
        => Write(LogLevel.Information, exception, messageTemplate, args);

    public void Warning(string messageTemplate)
        => Write(LogLevel.Warning, exception: null, messageTemplate, NoPropertyValues);

    public void Warning<T>(string messageTemplate, T property)
        => Write(LogLevel.Warning, exception: null, messageTemplate, property);

    public void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Warning, exception: null, messageTemplate, property0, property1);

    public void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Warning, exception: null, messageTemplate, property0, property1, property2);

    public void Warning(string messageTemplate, object[] args)
        => Write(LogLevel.Warning, exception: null, messageTemplate, args);

    public void Warning(Exception exception, string messageTemplate)
        => Write(LogLevel.Warning, exception, messageTemplate, NoPropertyValues);

    public void Warning<T>(Exception exception, string messageTemplate, T property)
        => Write(LogLevel.Warning, exception, messageTemplate, property);

    public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Warning, exception, messageTemplate, property0, property1);

    public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Warning, exception, messageTemplate, property0, property1, property2);

    public void Warning(Exception exception, string messageTemplate, object[] args)
        => Write(LogLevel.Warning, exception, messageTemplate, args);

    public void Error(string messageTemplate)
        => Write(LogLevel.Error, exception: null, messageTemplate, NoPropertyValues);

    public void Error<T>(string messageTemplate, T property)
        => Write(LogLevel.Error, exception: null, messageTemplate, property);

    public void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Error, exception: null, messageTemplate, property0, property1);

    public void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Error, exception: null, messageTemplate, property0, property1, property2);

    public void Error(string messageTemplate, object[] args)
        => Write(LogLevel.Error, exception: null, messageTemplate, args);

    public void Error(Exception exception, string messageTemplate)
        => Write(LogLevel.Error, exception, messageTemplate, NoPropertyValues);

    public void Error<T>(Exception exception, string messageTemplate, T property)
        => Write(LogLevel.Error, exception, messageTemplate, property);

    public void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1)
        => Write(LogLevel.Error, exception, messageTemplate, property0, property1);

    public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2)
        => Write(LogLevel.Error, exception, messageTemplate, property0, property1, property2);

    public void Error(Exception exception, string messageTemplate, object[] args)
        => Write(LogLevel.Error, exception, messageTemplate, args);

    private void Write<T>(LogLevel level, Exception? exception, string messageTemplate, T property)
    {
        if (IsEnabled(level))
        {
            // Avoid boxing + array allocation if disabled
            WriteImpl(level, exception, messageTemplate, new object?[] { property });
        }
    }

    private void Write<T0, T1>(LogLevel level, Exception? exception, string messageTemplate, T0 property0, T1 property1)
    {
        if (IsEnabled(level))
        {
            // Avoid boxing + array allocation if disabled
            WriteImpl(level, exception, messageTemplate, new object?[] { property0, property1 });
        }
    }

    private void Write<T0, T1, T2>(LogLevel level, Exception? exception, string messageTemplate, T0 property0, T1 property1, T2 property2)
    {
        if (IsEnabled(level))
        {
            // Avoid boxing + array allocation if disabled
            WriteImpl(level, exception, messageTemplate, new object?[] { property0, property1, property2 });
        }
    }

    private void Write(LogLevel level, Exception? exception, string messageTemplate, object[] args)
    {
        if (IsEnabled(level))
        {
            // logging is not disabled
            WriteImpl(level, exception, messageTemplate, args);
        }
    }

    private void WriteImpl(LogLevel level, Exception? exception, string messageTemplate, object?[] args)
    {
        try
        {
            var message =
                $"[{DateTime.UtcNow:O}] [{level}] {string.Format(messageTemplate, args)} {Environment.NewLine}";
            _sink.Write(message);

            if (exception != null)
            {
                var exceptionMessage = $"Exception: {exception.Message}{Environment.NewLine}{exception}{Environment.NewLine}";
                _sink.Write(exceptionMessage);
            }
        }
        catch
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
