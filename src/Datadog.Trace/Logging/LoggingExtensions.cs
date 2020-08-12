using System;

namespace Datadog.Trace.Logging
{
    internal static partial class LibLogExtensions
    {
        public static bool ErrorExceptionForFilter(this ILog logger, string message, Exception exception, params object[] formatParams)
        {
            logger.ErrorException(message, exception, formatParams);
            return false;
        }
    }
}
