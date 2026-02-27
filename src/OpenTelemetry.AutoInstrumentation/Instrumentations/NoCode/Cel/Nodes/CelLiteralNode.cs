// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a literal value (string, number, boolean, null).
/// </summary>
internal sealed class CelLiteralNode : CelNode
{
    private readonly object? _value;

    public CelLiteralNode(object? value)
    {
        _value = value;
    }

    public override object? Evaluate(NoCodeExpressionContext context) => _value;
}
