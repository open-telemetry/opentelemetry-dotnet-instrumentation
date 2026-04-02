// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck attribute where the underlying member is a field
/// </summary>
internal sealed class DuckFieldAttribute : DuckAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuckFieldAttribute"/> class.
    /// </summary>
    public DuckFieldAttribute()
    {
        Kind = DuckKind.Field;
    }
}
