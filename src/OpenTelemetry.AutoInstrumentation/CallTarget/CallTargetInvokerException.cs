// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

#pragma warning disable CA1032 // Implement standard exception constructors
#pragma warning disable CA1064 // Exceptions should be public. This exception is intended for internal use only.
internal class CallTargetInvokerException : Exception
#pragma warning restore CA1064 // Exceptions should be public. This exception is intended for internal use only.
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public CallTargetInvokerException(Exception innerException)
        : base(innerException.Message, innerException)
    {
    }
}
