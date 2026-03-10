// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a function call (e.g., substring("a", 0, 4)).
/// </summary>
internal sealed class CelFunctionCallNode : CelNode
{
    private readonly string _functionName;
    private readonly CelNode[] _arguments;

    public CelFunctionCallNode(string functionName, CelNode[] arguments)
    {
        _functionName = functionName;
        _arguments = arguments;
    }

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        return _functionName switch
        {
            "string" => EvaluateString(context),
            "size" => EvaluateSize(context),
            "startsWith" => EvaluateStartsWith(context),
            "endsWith" => EvaluateEndsWith(context),
            "contains" => EvaluateContains(context),
            _ => null
        };
    }

    private string EvaluateString(NoCodeExpressionContext context)
    {
        if (_arguments.Length != 1)
        {
            return string.Empty;
        }

        var value = _arguments[0].Evaluate(context);
        return value?.ToString() ?? string.Empty;
    }

    private object? EvaluateSize(NoCodeExpressionContext context)
    {
        if (_arguments.Length != 1)
        {
            return null;
        }

        var value = _arguments[0].Evaluate(context);
        return value switch
        {
            string s => s.Length,
            Array a => a.Length,
            System.Collections.ICollection c => c.Count,
            _ => null
        };
    }

    private bool EvaluateStartsWith(NoCodeExpressionContext context)
    {
        if (_arguments.Length != 2)
        {
            return false;
        }

        var str = _arguments[0].Evaluate(context)?.ToString();
        var prefix = _arguments[1].Evaluate(context)?.ToString();

        if (str == null || prefix == null)
        {
            return false;
        }

        return str.StartsWith(prefix, StringComparison.Ordinal);
    }

    private bool EvaluateEndsWith(NoCodeExpressionContext context)
    {
        if (_arguments.Length != 2)
        {
            return false;
        }

        var str = _arguments[0].Evaluate(context)?.ToString();
        var suffix = _arguments[1].Evaluate(context)?.ToString();

        if (str == null || suffix == null)
        {
            return false;
        }

        return str.EndsWith(suffix, StringComparison.Ordinal);
    }

    private bool EvaluateContains(NoCodeExpressionContext context)
    {
        if (_arguments.Length != 2)
        {
            return false;
        }

        var str = _arguments[0].Evaluate(context)?.ToString();
        var substring = _arguments[1].Evaluate(context)?.ToString();

        if (str == null || substring == null)
        {
            return false;
        }

#if NET
        return str.Contains(substring, StringComparison.Ordinal);
#else
        return str.IndexOf(substring, StringComparison.Ordinal) >= 0;
#endif
    }
}
