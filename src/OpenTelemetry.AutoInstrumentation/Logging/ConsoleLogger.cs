using System;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Logging
{
    internal class ConsoleLogger : ILogger
    {
        private static readonly object[] NoPropertyValues = Array.Empty<object>();

        private readonly string _name;

        public ConsoleLogger(string name)
        {
            _name = name;
        }

        public static ConsoleLogger Create<T>()
        {
            return Create(typeof(T));
        }

        public static ConsoleLogger Create(Type type)
        {
            return new ConsoleLogger(type.Name);
        }

        public bool IsEnabled(LogLevel level)
        {
            // Log all at the moment
            return true;
        }

        public void Debug(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception: null, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Debug<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception: null, messageTemplate, property, sourceLine, sourceFile);

        public void Debug<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception: null, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Debug<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception: null, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Debug(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception: null, messageTemplate, args, sourceLine, sourceFile);

        public void Debug(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Debug<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception, messageTemplate, property, sourceLine, sourceFile);

        public void Debug<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Debug<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Debug(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Debug, exception, messageTemplate, args, sourceLine, sourceFile);

        public void Information(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception: null, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Information<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception: null, messageTemplate, property, sourceLine, sourceFile);

        public void Information<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception: null, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Information<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception: null, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Information(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception: null, messageTemplate, args, sourceLine, sourceFile);

        public void Information(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Information<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception, messageTemplate, property, sourceLine, sourceFile);

        public void Information<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Information<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Information(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Information, exception, messageTemplate, args, sourceLine, sourceFile);

        public void Warning(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception: null, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Warning<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception: null, messageTemplate, property, sourceLine, sourceFile);

        public void Warning<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception: null, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Warning<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception: null, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Warning(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception: null, messageTemplate, args, sourceLine, sourceFile);

        public void Warning(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Warning<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception, messageTemplate, property, sourceLine, sourceFile);

        public void Warning<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Warning<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Warning(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Warning, exception, messageTemplate, args, sourceLine, sourceFile);

        public void Error(string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception: null, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Error<T>(string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception: null, messageTemplate, property, sourceLine, sourceFile);

        public void Error<T0, T1>(string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception: null, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Error<T0, T1, T2>(string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception: null, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Error(string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception: null, messageTemplate, args, sourceLine, sourceFile);

        public void Error(Exception exception, string messageTemplate, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception, messageTemplate, NoPropertyValues, sourceLine, sourceFile);

        public void Error<T>(Exception exception, string messageTemplate, T property, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception, messageTemplate, property, sourceLine, sourceFile);

        public void Error<T0, T1>(Exception exception, string messageTemplate, T0 property0, T1 property1, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception, messageTemplate, property0, property1, sourceLine, sourceFile);

        public void Error<T0, T1, T2>(Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception, messageTemplate, property0, property1, property2, sourceLine, sourceFile);

        public void Error(Exception exception, string messageTemplate, object[] args, [CallerLineNumber] int sourceLine = 0, [CallerFilePath] string sourceFile = "")
            => Write(LogLevel.Error, exception, messageTemplate, args, sourceLine, sourceFile);

        private void Write<T>(LogLevel level, Exception exception, string messageTemplate, T property, int sourceLine, string sourceFile)
        {
            if (IsEnabled(level))
            {
                // Avoid boxing + array allocation if disabled
                WriteImpl(level, exception, messageTemplate, new object[] { property }, sourceLine, sourceFile);
            }
        }

        private void Write<T0, T1>(LogLevel level, Exception exception, string messageTemplate, T0 property0, T1 property1, int sourceLine, string sourceFile)
        {
            if (IsEnabled(level))
            {
                // Avoid boxing + array allocation if disabled
                WriteImpl(level, exception, messageTemplate, new object[] { property0, property1 }, sourceLine, sourceFile);
            }
        }

        private void Write<T0, T1, T2>(LogLevel level, Exception exception, string messageTemplate, T0 property0, T1 property1, T2 property2, int sourceLine, string sourceFile)
        {
            if (IsEnabled(level))
            {
                // Avoid boxing + array allocation if disabled
                WriteImpl(level, exception, messageTemplate, new object[] { property0, property1, property2 }, sourceLine, sourceFile);
            }
        }

        private void Write(LogLevel level, Exception exception, string messageTemplate, object[] args, int sourceLine, string sourceFile)
        {
            if (IsEnabled(level))
            {
                // logging is not disabled
                WriteImpl(level, exception, messageTemplate, args, sourceLine, sourceFile);
            }
        }

        private void WriteImpl(LogLevel level, Exception exception, string messageTemplate, object[] args, int sourceLine, string sourceFile)
        {
            try
            {
                Console.WriteLine($"[{_name}] {level}:{string.Format(messageTemplate, args)}");

                if (exception != null)
                {
                    Console.WriteLine($">> Exception: {exception.Message} {Environment.NewLine} {exception}");
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
}
