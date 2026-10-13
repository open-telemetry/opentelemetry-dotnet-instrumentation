// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck type interface
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDuckType
{
    /// <summary>
    /// Gets instance
    /// </summary>
    object? Instance { get; }

    /// <summary>
    /// Gets instance Type
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the instance without boxing it to an object.
    /// </summary>
    /// <typeparam name="TReturn">The return type, which must be compatible with <see cref="Type"/>.</typeparam>
    /// <returns>A reference to the instance.</returns>
    ref TReturn GetInternalDuckTypedInstance<TReturn>();

    /// <summary>
    /// Calls ToString() on the instance
    /// </summary>
    /// <returns>ToString result</returns>
    string? ToString();
}
