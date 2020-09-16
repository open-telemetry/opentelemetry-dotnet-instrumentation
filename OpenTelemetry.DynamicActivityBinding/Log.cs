using System;

namespace OpenTelemetry.DynamicActivityBinding
{
    internal static class Log
    {
        /// <summary>
        /// We need to log these later properly, so that the info is available for support cases.
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            Console.WriteLine($"ERROR: {message ?? "null"}");
        }


        /// <summary>
        /// We need to log these later properly, so that the info is available for support cases.
        /// </summary>
        /// <param name="exception"></param>
        public static void Error(Exception exception)
        {
            Error(exception?.ToString());   
        }

        /// <summary>
        /// We need to log these later properly, so that the info is available for support cases.
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            Console.WriteLine($"INFO: {message ?? "null"}");
        }

        /// <summary>
        /// This is for debugging during prototyping. Most of calls to this are likely OK to be removed. (But double-check!)
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            Console.WriteLine($"DEBUG: {message ?? "null"}");
        }

        public static void Debug()
        {
            Debug(String.Empty);
        }

        internal static void Info(object p)
        {
            throw new NotImplementedException();
        }
    }
}
