// <copyright file="ThrowHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
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
    internal static void ThrowException(string message) => throw new Exception(message);

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
    internal static void ThrowNullReferenceException(string message) => throw new NullReferenceException(message);
}
