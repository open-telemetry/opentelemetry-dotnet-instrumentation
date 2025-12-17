// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Call target return value
/// </summary>
/// <typeparam name="T">Type of the return value</typeparam>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct CallTargetReturn<T>
{
    private readonly T _returnValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallTargetReturn{T}"/> struct.
    /// </summary>
    /// <param name="returnValue">Return value</param>
    public CallTargetReturn(T returnValue)
    {
        _returnValue = returnValue;
    }

    /// <summary>
    /// Gets the default call target return value (used by the native side to initialize the locals)
    /// </summary>
    /// <returns>Default call target return value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
#pragma warning disable CA1000 // Do not declare static members on generic types, needed for bytecode instrumentation
    public static CallTargetReturn<T> GetDefault()
#pragma warning restore CA1000 // Do not declare static members on generic types, needed for bytecode instrumentation
#pragma warning restore CA1024 // Use properties where appropriate
    {
        return default;
    }

    /// <summary>
    /// Gets the return value
    /// </summary>
    /// <returns>Return value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public T GetReturnValue() => _returnValue;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>
    /// ToString override
    /// </summary>
    /// <returns>String value</returns>
    public override string ToString()
    {
        return $"{typeof(CallTargetReturn<T>).FullName}({_returnValue})";
    }
}

/// <summary>
/// Call target return value
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct CallTargetReturn
{
    /// <summary>
    /// Gets the default call target return value (used by the native side to initialize the locals)
    /// </summary>
    /// <returns>Default call target return value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public static CallTargetReturn GetDefault()
#pragma warning restore CA1024 // Use properties where appropriate
    {
        return default;
    }
}
