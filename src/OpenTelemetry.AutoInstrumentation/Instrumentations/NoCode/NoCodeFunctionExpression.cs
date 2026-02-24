// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Represents a function expression that can combine multiple expressions.
/// Supports: concat(...), coalesce(...), substring(...), tostring(...)
/// </summary>
internal sealed class NoCodeFunctionExpression
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    // Pattern to match function calls: functionName(args)
    private static readonly Regex FunctionPattern = new(
        @"^(?<func>concat|coalesce|substring|tostring|isnull|isnotnull|equals|notequals)\s*\(\s*(?<args>.*)\s*\)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private NoCodeFunctionExpression(string functionName, object[] arguments, string rawExpression)
    {
        FunctionName = functionName;
        Arguments = arguments;
        RawExpression = rawExpression;
    }

    /// <summary>
    /// Gets the function name.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Gets the function arguments (can be NoCodeExpression, NoCodeFunctionExpression, or literal values).
    /// </summary>
    public object[] Arguments { get; }

    /// <summary>
    /// Gets the original raw expression string.
    /// </summary>
    public string RawExpression { get; }

    /// <summary>
    /// Tries to parse an expression as a function expression.
    /// </summary>
    public static NoCodeFunctionExpression? Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var trimmed = expression!.Trim();
        var match = FunctionPattern.Match(trimmed);
        if (!match.Success)
        {
            return null;
        }

        var functionName = match.Groups["func"].Value.ToUpperInvariant();
        var argsString = match.Groups["args"].Value;

        var arguments = ParseArguments(argsString);
        if (arguments == null)
        {
            Log.Debug("Failed to parse function arguments: '{0}'", expression);
            return null;
        }

        return new NoCodeFunctionExpression(functionName, arguments, expression);
    }

    /// <summary>
    /// Evaluates the function expression against the provided context.
    /// </summary>
    public object? Evaluate(NoCodeExpressionContext context)
    {
        return FunctionName switch
        {
            "CONCAT" => EvaluateConcat(context),
            "COALESCE" => EvaluateCoalesce(context),
            "SUBSTRING" => EvaluateSubstring(context),
            "TOSTRING" => EvaluateToString(context),
            "ISNULL" => EvaluateIsNull(context),
            "ISNOTNULL" => EvaluateIsNotNull(context),
            "EQUALS" => EvaluateEquals(context),
            "NOTEQUALS" => EvaluateNotEquals(context),
            _ => null
        };
    }

    private static object[]? ParseArguments(string argsString)
    {
        if (string.IsNullOrWhiteSpace(argsString))
        {
            return [];
        }

        var arguments = new List<object>();
        var currentArg = new System.Text.StringBuilder();
        var depth = 0;
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < argsString.Length; i++)
        {
            var c = argsString[i];

            // Handle string literals
            if ((c == '"' || c == '\'') && (i == 0 || argsString[i - 1] != '\\'))
            {
                if (!inString)
                {
                    inString = true;
                    stringChar = c;
                }
                else if (c == stringChar)
                {
                    inString = false;
                }

                currentArg.Append(c);
                continue;
            }

            if (inString)
            {
                currentArg.Append(c);
                continue;
            }

            // Handle nested parentheses
            if (c == '(')
            {
                depth++;
                currentArg.Append(c);
            }
            else if (c == ')')
            {
                depth--;
                currentArg.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                // Argument separator
                var arg = ParseSingleArgument(currentArg.ToString().Trim());
                if (arg != null)
                {
                    arguments.Add(arg);
                }

                currentArg.Clear();
            }
            else
            {
                currentArg.Append(c);
            }
        }

        // Last argument
        if (currentArg.Length > 0)
        {
            var arg = ParseSingleArgument(currentArg.ToString().Trim());
            if (arg != null)
            {
                arguments.Add(arg);
            }
        }

        return arguments.ToArray();
    }

    private static object? ParseSingleArgument(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            return null;
        }

        // Check for string literal
#if  NET
        if ((arg.StartsWith('"') && arg.EndsWith('"')) ||
            (arg.StartsWith('\'') && arg.EndsWith('\'')))
#else
        if ((arg.StartsWith("\"", StringComparison.Ordinal) && arg.EndsWith("\"", StringComparison.Ordinal)) ||
            (arg.StartsWith("\'", StringComparison.Ordinal) && arg.EndsWith("\'", StringComparison.Ordinal)))
#endif
        {
            return arg.Substring(1, arg.Length - 2);
        }

        // Check for numeric literal
        if (int.TryParse(arg, out var intVal))
        {
            return intVal;
        }

        if (double.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
        {
            return doubleVal;
        }

        // Check for boolean literal
        if (bool.TryParse(arg, out var boolVal))
        {
            return boolVal;
        }

        // Check for nested function
        var funcExpr = Parse(arg);
        if (funcExpr != null)
        {
            return funcExpr;
        }

        // Check for variable expression
        var varExpr = NoCodeExpression.Parse(arg);
        if (varExpr != null)
        {
            return varExpr;
        }

        // Return as literal string
        return arg;
    }

    private static object? EvaluateArgument(object arg, NoCodeExpressionContext context)
    {
        return arg switch
        {
            NoCodeExpression expr => expr.Evaluate(context),
            NoCodeFunctionExpression func => func.Evaluate(context),
            _ => arg
        };
    }

    private string? EvaluateConcat(NoCodeExpressionContext context)
    {
        var parts = new List<string>();
        foreach (var arg in Arguments)
        {
            var value = EvaluateArgument(arg, context);
            if (value != null)
            {
                parts.Add(value.ToString() ?? string.Empty);
            }
        }

        return string.Concat(parts);
    }

    private object? EvaluateCoalesce(NoCodeExpressionContext context)
    {
        foreach (var arg in Arguments)
        {
            var value = EvaluateArgument(arg, context);
            if (value != null)
            {
                if (value is string str && !string.IsNullOrEmpty(str))
                {
                    return value;
                }

                if (value is not string)
                {
                    return value;
                }
            }
        }

        return null;
    }

    private string? EvaluateSubstring(NoCodeExpressionContext context)
    {
        if (Arguments.Length < 2)
        {
            return null;
        }

        var strValue = EvaluateArgument(Arguments[0], context)?.ToString();
        if (string.IsNullOrEmpty(strValue))
        {
            return null;
        }

        var startValue = EvaluateArgument(Arguments[1], context);
        if (startValue is not int start)
        {
            if (startValue is long startLong)
            {
                start = (int)startLong;
            }
            else
            {
                return null;
            }
        }

        if (start < 0 || start >= strValue!.Length)
        {
            return null;
        }

        if (Arguments.Length >= 3)
        {
            var lengthValue = EvaluateArgument(Arguments[2], context);
            if (lengthValue is int length || (lengthValue is long lengthLong && (length = (int)lengthLong) >= 0))
            {
                var maxLength = Math.Min(length, strValue.Length - start);
                return strValue.Substring(start, maxLength);
            }
        }

        return strValue.Substring(start);
    }

    private string? EvaluateToString(NoCodeExpressionContext context)
    {
        if (Arguments.Length == 0)
        {
            return null;
        }

        var value = EvaluateArgument(Arguments[0], context);
        return value?.ToString();
    }

    private bool EvaluateIsNull(NoCodeExpressionContext context)
    {
        if (Arguments.Length == 0)
        {
            return true;
        }

        var value = EvaluateArgument(Arguments[0], context);
        return value == null || (value is string str && string.IsNullOrEmpty(str));
    }

    private bool EvaluateIsNotNull(NoCodeExpressionContext context)
    {
        return !EvaluateIsNull(context);
    }

    private bool EvaluateEquals(NoCodeExpressionContext context)
    {
        if (Arguments.Length < 2)
        {
            return false;
        }

        var left = EvaluateArgument(Arguments[0], context);
        var right = EvaluateArgument(Arguments[1], context);

        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        return left.Equals(right) || left.ToString() == right.ToString();
    }

    private bool EvaluateNotEquals(NoCodeExpressionContext context)
    {
        return !EvaluateEquals(context);
    }
}
