using System;

namespace OpenTelemetry.ClrProfiler.CallTarget
{
    internal class CallTargetInvokerException : Exception
    {
        public CallTargetInvokerException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
