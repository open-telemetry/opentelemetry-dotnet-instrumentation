using OpenTelemetry.Util;
using System;

namespace OpenTelemetry.DynamicActivityBinding
{
    /// <summary>
    /// Vendors using this librry can change the implementaton of the APIs in this class to plug in whatevcer loging solution they wish.
    /// We will avoid creating a complex logging abstraction or taking dependencies on ILogger for now.
    /// </summary>
    internal static class Log
    {
        private const string TimestampFormat = "yy-MM-dd, HH:mm:ss.fff";
        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            Console.WriteLine();
            Console.WriteLine($"[{DateTimeOffset.Now.ToString(TimestampFormat)} | ERROR] {Format.SpellIfNull(message)}");
        }


        /// <summary>
        /// Logs an error.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        /// <param name="exception"></param>
        public static void Error(Exception exception)
        {
            Error(exception?.ToString());   
        }

        /// <summary>
        /// Logs an important info message.
        /// These need to be persisted well, so that the info is available for support cases.
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            Console.WriteLine();
            Console.WriteLine($"[{DateTimeOffset.Now.ToString(TimestampFormat)} | INFO]  {Format.SpellIfNull(message)}");
        }

        /// <summary>
        /// Logs a non-critical info message. Mainly used for for debugging during prototyping.
        /// These messages can likely be dropped in production.
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            Console.WriteLine();
            Console.WriteLine($"[{DateTimeOffset.Now.ToString(TimestampFormat)} | DEBUG] {Format.SpellIfNull(message)}");
        }

        public static void Debug()
        {
            Debug(String.Empty);
        }
    }
}
