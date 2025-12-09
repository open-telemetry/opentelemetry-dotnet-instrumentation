// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

#pragma warning disable CA1032 // Implement standard exception constructors
internal class CallTargetInvokerException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public CallTargetInvokerException(Exception innerException)
        : base(innerException.Message, innerException)
    {
    }
}
