// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents member access (e.g., instance.property).
/// </summary>
internal sealed class CelMemberAccessNode : CelNode
{
    private readonly CelNode _target;
    private readonly string _memberName;

    public CelMemberAccessNode(CelNode target, string memberName)
    {
        _target = target;
        _memberName = memberName;
    }

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        var target = _target.Evaluate(context);
        if (target == null)
        {
            return null;
        }

        return CelExpression.GetPropertyValue(target, _memberName);
    }
}
