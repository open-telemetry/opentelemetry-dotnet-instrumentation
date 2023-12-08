// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal class ContinuationGenerator<TTarget, TReturn>
{
    public virtual TReturn? SetContinuation(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
    {
        return returnValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static TReturn ToTReturn<TFrom>(TFrom returnValue)
    {
#if NET6_0_OR_GREATER
        return Unsafe.As<TFrom, TReturn>(ref returnValue);
#else
        return ContinuationsHelper.Convert<TFrom, TReturn>(returnValue);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static TTo FromTReturn<TTo>(TReturn? returnValue)
    {
#if NET6_0_OR_GREATER
        return Unsafe.As<TReturn?, TTo>(ref returnValue);
#else
        return ContinuationsHelper.Convert<TReturn?, TTo>(returnValue);
#endif
    }
}
