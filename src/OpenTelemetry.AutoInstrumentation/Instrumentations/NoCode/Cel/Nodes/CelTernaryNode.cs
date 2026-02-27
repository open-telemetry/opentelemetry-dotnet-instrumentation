// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a ternary conditional operator (condition ? trueValue : falseValue).
/// </summary>
internal sealed class CelTernaryNode : CelNode
{
    private readonly CelNode _condition;
    private readonly CelNode _trueValue;
    private readonly CelNode _falseValue;

    public CelTernaryNode(CelNode condition, CelNode trueValue, CelNode falseValue)
    {
        _condition = condition;
        _trueValue = trueValue;
        _falseValue = falseValue;
    }

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        var condition = _condition.Evaluate(context);
        var isTrue = condition switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            long l => l != 0,
            null => false,
            _ => true
        };

        return isTrue ? _trueValue.Evaluate(context) : _falseValue.Evaluate(context);
    }
}
