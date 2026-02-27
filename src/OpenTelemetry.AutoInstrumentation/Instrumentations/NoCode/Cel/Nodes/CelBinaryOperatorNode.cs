// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Represents a binary operator (e.g., ==, !=, &lt;, &gt;, &amp;&amp;, ||).
/// </summary>
internal sealed class CelBinaryOperatorNode : CelNode
{
    private readonly CelNode _left;
    private readonly CelNode _right;
    private readonly string _operator;

    public CelBinaryOperatorNode(CelNode left, string @operator, CelNode right)
    {
        _left = left;
        _operator = @operator;
        _right = right;
    }

    public override object? Evaluate(NoCodeExpressionContext context)
    {
        var left = _left.Evaluate(context);
        var right = _right.Evaluate(context);

        return _operator switch
        {
            "==" => AreEqual(left, right),
            "!=" => !AreEqual(left, right),
            "<" => CompareLessThan(left, right),
            ">" => CompareGreaterThan(left, right),
            "<=" => !CompareGreaterThan(left, right),
            ">=" => !CompareLessThan(left, right),
            "&&" => IsTrue(left) && IsTrue(right),
            "||" => IsTrue(left) || IsTrue(right),
            "+" => Add(left, right),
            "-" => Subtract(left, right),
            "*" => Multiply(left, right),
            "/" => Divide(left, right),
            "%" => Modulo(left, right),
            _ => null
        };
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        // Numeric comparison
        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return Math.Abs(leftNum - rightNum) < double.Epsilon;
        }

        return left.Equals(right);
    }

    private static bool CompareLessThan(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return leftNum < rightNum;
        }

        if (left is string leftStr && right is string rightStr)
        {
            return string.CompareOrdinal(leftStr, rightStr) < 0;
        }

        return false;
    }

    private static bool CompareGreaterThan(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return leftNum > rightNum;
        }

        if (left is string leftStr && right is string rightStr)
        {
            return string.CompareOrdinal(leftStr, rightStr) > 0;
        }

        return false;
    }

    private static bool IsNumeric(object value)
    {
        return value is int or long or float or double or decimal or byte or short or uint or ulong or ushort or sbyte;
    }

    private static bool IsTrue(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            long l => l != 0,
            double d => Math.Abs(d) > double.Epsilon,
            float f => Math.Abs(f) > float.Epsilon,
            null => false,
            _ => true
        };
    }

    private static object? Add(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return leftNum + rightNum;
        }

        return null;
    }

    private static object? Subtract(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return leftNum - rightNum;
        }

        return null;
    }

    private static object? Multiply(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            return leftNum * rightNum;
        }

        return null;
    }

    private static object? Divide(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            if (Math.Abs(rightNum) < double.Epsilon)
            {
                return null; // Division by zero
            }

            return leftNum / rightNum;
        }

        return null;
    }

    private static object? Modulo(object? left, object? right)
    {
        if (left == null || right == null)
        {
            return null;
        }

        if (IsNumeric(left) && IsNumeric(right))
        {
            var leftNum = Convert.ToDouble(left, CultureInfo.InvariantCulture);
            var rightNum = Convert.ToDouble(right, CultureInfo.InvariantCulture);
            if (Math.Abs(rightNum) < double.Epsilon)
            {
                return null; // Modulo by zero
            }

            return leftNum % rightNum;
        }

        return null;
    }
}
