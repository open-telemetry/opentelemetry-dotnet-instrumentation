// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents an identifier (variable name).
/// </summary>
internal sealed class CelIdentifierNode : CelNode
{
    private readonly string _name;

    public CelIdentifierNode(string name)
    {
        _name = name;
    }

    internal string RawExpression => _name;

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        return _name switch
        {
            "instance" => context.Instance,
            "return" => context.ReturnValue,
            "method" => context.MethodName,
            "type" => context.TypeName,
            "arguments" => context.Arguments,
            _ => null
        };
    }
}
