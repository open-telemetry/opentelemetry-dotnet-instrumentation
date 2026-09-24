// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal abstract class ContinuationGenerator<TTarget, TReturn>
{
    internal static readonly IOtelLogger Log = OtelLogging.GetLogger();

    internal delegate object? ObjectContinuationMethodDelegate(TTarget target, object? returnValue, Exception? exception, in CallTargetState state);

    internal delegate Task<object?> AsyncObjectContinuationMethodDelegate(TTarget target, object? returnValue, Exception? exception, in CallTargetState state);

    public abstract TReturn? SetContinuation(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static TReturn ToTReturn<TFrom>(TFrom returnValue)
    {
#if NET
        return Unsafe.As<TFrom, TReturn>(ref returnValue);
#else
        return ContinuationsHelper.Convert<TFrom, TReturn>(returnValue);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static TTo FromTReturn<TTo>(TReturn? returnValue)
    {
#if NET
        return Unsafe.As<TReturn?, TTo>(ref returnValue);
#else
        return ContinuationsHelper.Convert<TReturn?, TTo>(returnValue);
#endif
    }

    internal abstract class CallbackHandler
    {
        public abstract TReturn? ExecuteCallback(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state);
    }

    internal class NoOpCallbackHandler : CallbackHandler
    {
        public override TReturn? ExecuteCallback(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
        {
            return returnValue;
        }
    }
}

#pragma warning disable SA1402 // File may only contain a single type
internal abstract class ContinuationGenerator<TTarget, TReturn, TResult> : ContinuationGenerator<TTarget, TReturn>
#pragma warning restore SA1402 // File may only contain a single type
{
    internal delegate TResult? ContinuationMethodDelegate(TTarget target, TResult? returnValue, Exception? exception, in CallTargetState state);

    internal delegate Task<TResult?> AsyncContinuationMethodDelegate(TTarget target, TResult? returnValue, Exception? exception, in CallTargetState state);
}
