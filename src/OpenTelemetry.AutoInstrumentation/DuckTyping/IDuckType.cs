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
    object Instance { get; }

    /// <summary>
    /// Gets instance Type
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Calls ToString() on the instance
    /// </summary>
    /// <returns>ToString result</returns>
    string ToString();
}
