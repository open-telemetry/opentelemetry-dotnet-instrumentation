using System;

namespace OpenTelemetry.AutoInstrumentation.CallTarget
{
    internal class CallTargetInvokerException : Exception
    {
        public CallTargetInvokerException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }
    }
}
