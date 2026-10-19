// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Call target ref struct container
/// </summary>
/// <remarks>The stack pointer is valid only during the synchronous CallTarget callback.</remarks>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CA1815 // This opaque stack view is not a value for equality comparisons. Preserve the upstream contract.
public readonly struct CallTargetRefStruct
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
    private readonly nint _value;
    private readonly RuntimeTypeHandle _refStructTypeHandle;

    private CallTargetRefStruct(nint value, RuntimeTypeHandle refStructTypeHandle)
    {
        _value = value;
        _refStructTypeHandle = refStructTypeHandle;
    }

    /// <summary>
    /// Gets stack pointer of the ref struct instance
    /// </summary>
    public nint Value => _value;

    /// <summary>
    /// Gets the Ref struct type
    /// </summary>
    public Type? StructType => Type.GetTypeFromHandle(_refStructTypeHandle);

    /// <summary>
    /// Gets the generic type of the ref struct
    /// </summary>
    public Type? GenericType => StructType?.IsGenericType == true ? StructType.GetGenericTypeDefinition() : null;

    /// <summary>
    /// Gets the first generic parameter of the ref struct
    /// </summary>
    public Type? ElementType => StructType?.IsGenericType == true ? StructType.GetGenericArguments()[0] : null;

    /// <summary>
    /// Create a new instance of <see cref="CallTargetRefStruct"/>
    /// </summary>
    /// <param name="refStructPointer">Stack pointer of the ref struct instance</param>
    /// <param name="refStructTypeHandle">Runtime type handle of the ref struct</param>
    /// <returns>A new instance of the CallTargetRefStruct container</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe CallTargetRefStruct Create(void* refStructPointer, RuntimeTypeHandle refStructTypeHandle)
        => new((nint)refStructPointer, refStructTypeHandle);

#if NET
    /// <summary>
    /// Gets a read-only span from the ref struct instance
    /// </summary>
    /// <param name="success">True if the ref struct is a ReadOnlySpan</param>
    /// <typeparam name="T">Type of the read-only span</typeparam>
    /// <returns>Reference to the same ReadOnlySpan instance of the caller</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref ReadOnlySpan<T> DangerousGetReadOnlySpan<T>(out bool success)
    {
        if (Type.GetTypeFromHandle(_refStructTypeHandle) == typeof(ReadOnlySpan<T>))
        {
            success = true;
            return ref (*(ReadOnlySpan<T>*)_value);
        }

        success = false;
        // Null pointer (same code as Unsafe.NullRef)
        return ref (*(ReadOnlySpan<T>*)null);
    }

    /// <summary>
    /// Gets a read-only span from the ref struct instance
    /// </summary>
    /// <param name="success">True if the ref struct is a ReadOnlySpan</param>
    /// <typeparam name="T">Type of the read-only span</typeparam>
    /// <returns>Reference to the same ReadOnlySpan instance of the caller</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly ReadOnlySpan<T> GetReadOnlySpan<T>(out bool success)
    {
        return ref DangerousGetReadOnlySpan<T>(out success);
    }
#endif

#if NET
    /// <summary>
    /// Gets a span from the ref struct instance
    /// </summary>
    /// <param name="success">True if the ref struct is a Span</param>
    /// <typeparam name="T">Type of the span</typeparam>
    /// <returns>Reference to the same Span instance of the caller</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ref Span<T> DangerousGetSpan<T>(out bool success)
    {
        if (Type.GetTypeFromHandle(_refStructTypeHandle) == typeof(Span<T>))
        {
            success = true;
            return ref (*(Span<T>*)_value);
        }

        success = false;
        // Null pointer (same code as Unsafe.NullRef)
        return ref (*(Span<T>*)null);
    }

    /// <summary>
    /// Gets a span from the ref struct instance
    /// </summary>
    /// <param name="success">True if the ref struct is a Span</param>
    /// <typeparam name="T">Type of the span</typeparam>
    /// <returns>Reference to the same Span instance of the caller</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly Span<T> GetSpan<T>(out bool success)
    {
        return ref DangerousGetSpan<T>(out success);
    }
#endif

}
