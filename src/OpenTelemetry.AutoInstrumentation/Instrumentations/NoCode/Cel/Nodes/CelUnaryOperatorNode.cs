// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a unary operator (e.g., !).
/// </summary>
internal sealed class CelUnaryOperatorNode : CelNode
{
    private readonly CelNode _operand;
    private readonly string _operator;

    public CelUnaryOperatorNode(string @operator, CelNode operand)
    {
        _operator = @operator;
        _operand = operand;
    }

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        var operand = _operand.Evaluate(context);

        return _operator switch
        {
            "!" => !IsTrue(operand),
            "-" => Negate(operand),
            _ => null
        };
    }

    private static bool IsTrue(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            long l => l != 0,
            null => false,
            _ => true
        };
    }

    private static object? Negate(object? value)
    {
        return value switch
        {
            int i => -i,
            long l => -l,
            float f => -f,
            double d => -d,
            decimal dec => -dec,
            _ => null
        };
    }
}
