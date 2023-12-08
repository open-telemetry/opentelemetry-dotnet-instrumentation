// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Emit;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal readonly struct CreateAsyncEndMethodResult
{
    public readonly DynamicMethod Method;
    public readonly bool PreserveContext;

    public CreateAsyncEndMethodResult(DynamicMethod method, bool preserveContext)
    {
        Method = method;
        PreserveContext = preserveContext;
    }
}
