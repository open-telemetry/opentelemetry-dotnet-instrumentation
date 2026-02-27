// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a function call (e.g., concat("a", "b")).
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
        return _functionName.ToUpperInvariant() switch
        {
            "CONCAT" => EvaluateConcat(context),
            "COALESCE" => EvaluateCoalesce(context),
            "SUBSTRING" => EvaluateSubstring(context),
            "STRING" => EvaluateString(context),
            "SIZE" => EvaluateSize(context),
            "STARTSWITH" => EvaluateStartsWith(context),
            "ENDSWITH" => EvaluateEndsWith(context),
            "CONTAINS" => EvaluateContains(context),
            _ => null
        };
    }

    private string EvaluateConcat(NoCodeExpressionContext context)
    {
        var sb = new StringBuilder();
        foreach (var arg in _arguments)
        {
            var value = arg.Evaluate(context);
            if (value != null)
            {
                sb.Append(value);
            }
        }

        return sb.ToString();
    }

    private object? EvaluateCoalesce(NoCodeExpressionContext context)
    {
        foreach (var arg in _arguments)
        {
            var value = arg.Evaluate(context);
            if (value != null)
            {
                return value;
            }
        }

        return null;
    }

    private string EvaluateSubstring(NoCodeExpressionContext context)
    {
        if (_arguments.Length < 2 || _arguments.Length > 3)
        {
            return string.Empty;
        }

        var str = _arguments[0].Evaluate(context)?.ToString();
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        var startObj = _arguments[1].Evaluate(context);
        if (startObj is not int start)
        {
            return string.Empty;
        }

        // Handle out of range start index
        if (start < 0)
        {
            return string.Empty;
        }

        if (start >= str!.Length)
        {
            return string.Empty;
        }

        if (_arguments.Length == 3)
        {
            var lengthObj = _arguments[2].Evaluate(context);
            if (lengthObj is not int length)
            {
                return string.Empty;
            }

            if (length < 0)
            {
                return string.Empty;
            }

            // Clamp length to not exceed string bounds
            var maxLength = str.Length - start;
            var actualLength = Math.Min(length, maxLength);

            return str.Substring(start, actualLength);
        }

        return str.Substring(start);
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
