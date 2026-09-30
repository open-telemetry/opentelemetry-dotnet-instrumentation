// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// A duck typed value together with the original declared type.
/// </summary>
/// <typeparam name="TProxy">The duck type proxy.</typeparam>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable CA1815 // Equality is not part of the DataDog duck typing contract.
public readonly struct ValueWithType<TProxy>
#pragma warning restore CA1815 // Equality is not part of the DataDog duck typing contract.
{
    /// <summary>
    /// The duck typed value.
    /// </summary>
#pragma warning disable CA1051 // Public fields are required by the emitted duck typing IL.
    public readonly TProxy? Value;

    /// <summary>
    /// The original declared type.
    /// </summary>
    public readonly Type Type;
#pragma warning restore CA1051 // Public fields are required by the emitted duck typing IL.

    private ValueWithType(TProxy? value, Type type)
    {
        Value = value;
        Type = type;
    }

    /// <summary>
    /// Creates a value with its original declared type.
    /// </summary>
    /// <param name="value">The duck typed value.</param>
    /// <param name="type">The original declared type.</param>
    /// <returns>The value and type.</returns>
#pragma warning disable CA1000 // The generic factory matches the DataDog duck typing contract.
    public static ValueWithType<TProxy> Create(TProxy? value, Type type)
#pragma warning restore CA1000 // The generic factory matches the DataDog duck typing contract.
    {
        return new ValueWithType<TProxy>(value, type);
    }
}
