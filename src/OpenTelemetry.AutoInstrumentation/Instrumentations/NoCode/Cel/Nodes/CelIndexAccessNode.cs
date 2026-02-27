// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents array/list indexing (e.g., arguments[0]).
/// </summary>
internal sealed class CelIndexAccessNode : CelNode
{
    private readonly CelNode _target;
    private readonly CelNode _index;

    public CelIndexAccessNode(CelNode target, CelNode index)
    {
        _target = target;
        _index = index;
    }

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        var target = _target.Evaluate(context);
        if (target == null)
        {
            return null;
        }

        var indexValue = _index.Evaluate(context);
        if (indexValue is not int idx)
        {
            return null;
        }

        return CelExpression.GetIndexValue(target, idx);
    }
}
