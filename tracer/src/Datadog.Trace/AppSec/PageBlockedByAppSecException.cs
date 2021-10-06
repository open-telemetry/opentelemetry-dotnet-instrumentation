using System;
using System.Runtime.Serialization;

namespace Datadog.Trace.AppSec
{
    /// <summary>
    /// This exception should only be used to signal to the end user that
    /// there page has been blocked
    /// </summary>
    internal class PageBlockedByAppSecException : Exception
    {
        public PageBlockedByAppSecException()
        {
        }

        public PageBlockedByAppSecException(string message)
            : base(message)
        {
        }

        public PageBlockedByAppSecException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PageBlockedByAppSecException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
