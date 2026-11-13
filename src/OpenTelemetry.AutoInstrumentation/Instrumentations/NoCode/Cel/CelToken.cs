// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a token in a CEL expression.
/// </summary>
internal readonly struct CelToken
{
    public CelToken(CelTokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public CelTokenType Type { get; }

    public string Value { get; }

    public int Position { get; }
}
