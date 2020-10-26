using System;
using System.Runtime.CompilerServices;
using System.Text;
using Datadog.Util;

namespace OpenTelemetry.AutoInstrumentation.ActivityCollector
{
    /// <summary>
    /// Vendors using this library can change the implementaton of the APIs in this class to plug in whatevcer loging solution they wish.
    /// We will avoid creating a complex logging abstraction or taking dependencies on ILogger for now.
    /// </summary>
    public static class Log
    {
        private static class Default
        {
            private const string TimestampFormat = "yy-MM-dd, HH:mm:ss.fff";

            public static void ErrorMessage(string componentName, string message, params object[] dataNamesAndValues)
            {
                Console.WriteLine();
                Console.WriteLine($"[{DateTimeOffset.Now.ToString(TimestampFormat)} | ERROR] {FormatEventInfo(componentName, message, dataNamesAndValues)}");
            }

            public static void ErrorException(string componentName, Exception exception, params object[] dataNamesAndValues)
            {
                Log.Error(componentName, exception?.ToString(), dataNamesAndValues);
            }

            public static void Info(string componentName, string message, params object[] dataNamesAndValues)
            {
                Console.WriteLine();
                Console.WriteLine($"[{DateTimeOffset.Now.ToString(TimestampFormat)} | INFO]  {FormatEventInfo(componentName, message, dataNamesAndValues)}");
            }

            public static void Debug(string componentName, string message, params object[] dataNamesAndValues)
            {
                Console.WriteLine();
                Console.WriteLine($"[{DateTimeOffset.Now.ToString(TimestampFormat)} | DEBUG] {FormatEventInfo(componentName, message, dataNamesAndValues)}");
            }

            private static string FormatEventInfo(string componentName, string message, params object[] dataNamesAndValues)
            {
                var s = new StringBuilder();

                if (!String.IsNullOrWhiteSpace(componentName))
                {
                    s.Append(componentName);
                    s.Append(": ");
                }

                if (!String.IsNullOrWhiteSpace(message))
                {
                    s.Append(message);
                    s.Append(". ");
                }

                if (dataNamesAndValues != null && dataNamesAndValues.Length > 0)
                {
                    s.Append("{");
                    for (int i = 0; i < dataNamesAndValues.Length; i += 2)
                    {
                        s.Append('[');
                        s.Append(Format.QuoteIfString(dataNamesAndValues[i]));
                        s.Append(']');
                        s.Append('=');
                        
                        i++;
                        if (i< dataNamesAndValues.Length)
                        {
                            s.Append(i < dataNamesAndValues.Length ? Format.QuoteIfString(dataNamesAndValues[i]) : "unspecified");
                        }
                    }
                    s.Append("}");
                }

                return s.ToString();
            }
        }  // class Default

        public static class Configure
        {
            public static void Error(Action<string, string, object[]> logEventHandler)
            {
                s_errorMessageLogEventHandler = logEventHandler;
            }

            public static void Error(Action<string, Exception, object[]> logEventHandler)
            {
                s_errorExceptionLogEventHandler = logEventHandler;
            }

            public static void Info(Action<string, string, object[]> logEventHandler)
            {
                s_infoLogEventHandler = logEventHandler;
            }

            public static void Debug(Action<string, string, object[]> logEventHandler)
            {
                s_debugLogEventHandler = logEventHandler;
            }
        }

        private static Action<string, string, object[]> s_errorMessageLogEventHandler = Default.ErrorMessage;
        private static Action<string, Exception, object[]> s_errorExceptionLogEventHandler = Default.ErrorException;
        private static Action<string, string, object[]> s_infoLogEventHandler = Default.Info;
        private static Action<string, string, object[]> s_debugLogEventHandler = Default.Debug;

        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, string message, params object[] dataNamesAndValues)
        {
            Action<string, string, object[]> logEventHandler = s_errorMessageLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, dataNamesAndValues);
            }
        }


        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string componentName, Exception exception, params object[] dataNamesAndValues)
        {
            Action<string, Exception, object[]> logEventHandler = s_errorExceptionLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, exception, dataNamesAndValues);
            }
        }

        /// <summary>
        /// Logs an important info message.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string componentName, string message, params object[] dataNamesAndValues)
        {
            Action<string, string, object[]> logEventHandler = s_infoLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, dataNamesAndValues);
            }
        }

        /// <summary>
        /// Logs a non-critical info message. Mainly used for for debugging during prototyping.
        /// These messages can likely be dropped in production.
        /// </summary>
        /// <param name="message"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(string componentName, string message, params object[] dataNamesAndValues)
        {
            Action<string, string, object[]> logEventHandler = s_debugLogEventHandler;
            if (logEventHandler != null)
            {
                logEventHandler(componentName, message, dataNamesAndValues);
            }
        }
    }
}
