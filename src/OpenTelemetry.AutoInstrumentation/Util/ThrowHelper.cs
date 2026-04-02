// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.Util;

internal class ThrowHelper
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(string paramName) => throw new ArgumentOutOfRangeException(paramName);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(string paramName, string message) => throw new ArgumentOutOfRangeException(paramName, message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(string paramName, object actualValue, string message) => throw new ArgumentOutOfRangeException(paramName, actualValue, message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowArgumentException(string message) => throw new ArgumentException(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowArgumentException(string message, string paramName) => throw new ArgumentException(message, paramName);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowInvalidOperationException(string message) => throw new InvalidOperationException(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowInvalidCastException(string message) => throw new InvalidCastException(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowNotSupportedException(string message) => throw new NotSupportedException(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
    internal static void ThrowKeyNotFoundException(string message) => throw new KeyNotFoundException(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DebuggerHidden]
    [DoesNotReturn]
#pragma warning disable CA2201 // Do not raise reserved exception types. Needed in DuckTyping scenarios.
    internal static void ThrowNullReferenceException(string message) => throw new NullReferenceException(message);
#pragma warning restore CA2201 // Do not raise reserved exception types. Needed in DuckTyping scenarios.
}
