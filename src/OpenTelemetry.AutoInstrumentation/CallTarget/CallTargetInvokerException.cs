// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

internal class CallTargetInvokerException : Exception
{
    public CallTargetInvokerException(Exception innerException)
        : base(innerException.Message, innerException)
    {
    }
}
