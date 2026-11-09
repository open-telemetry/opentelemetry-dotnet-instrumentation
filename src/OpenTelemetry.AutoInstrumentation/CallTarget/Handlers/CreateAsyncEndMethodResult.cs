// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Emit;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal readonly struct CreateAsyncEndMethodResult
{
    public readonly DynamicMethod? Method;
    public readonly bool PreserveContext;

    /// <summary>
    /// Whether the integration's OnAsyncMethodEnd callback is itself asynchronous.
    /// </summary>
    /// <remarks>
    /// This cannot be inferred from <see cref="Method"/>: for a runtime-async target returning
    /// Task&lt;Task&gt;, a synchronous callback also has Task as its generated return type.
    /// </remarks>
    public readonly bool IsTaskReturn;

    public CreateAsyncEndMethodResult(DynamicMethod method, bool preserveContext, bool isTaskReturn)
    {
        Method = method;
        PreserveContext = preserveContext;
        IsTaskReturn = isTaskReturn;
    }
}
