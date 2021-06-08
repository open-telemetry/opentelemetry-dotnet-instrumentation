using System;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.CallTarget
{
    internal class CallTargetInvokerException : Exception
    {
        public CallTargetInvokerException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
