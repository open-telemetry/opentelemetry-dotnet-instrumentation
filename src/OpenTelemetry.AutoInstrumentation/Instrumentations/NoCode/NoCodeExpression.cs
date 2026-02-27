// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Represents a parsed expression for extracting values from method context.
/// Supports expressions like: $arg1, $arg2.Property, $instance.Property.Nested, $return, $method, $type
/// </summary>
internal sealed class NoCodeExpression
{
    private const int MaxPropertyDepth = 10;

    private static readonly IOtelLogger Log = OtelLogging.GetLogger();
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();
    private static readonly Regex ExpressionPattern = new(@"^\$(?<source>arg(?<index>\d+)|instance|return|method|type)(?<path>(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)$", RegexOptions.Compiled);

    private NoCodeExpression(NoCodeExpressionType type, int? argumentIndex, string[] propertyPath, string rawExpression)
    {
        Type = type;
        ArgumentIndex = argumentIndex;
        PropertyPath = propertyPath;
        RawExpression = rawExpression;
    }

    /// <summary>
    /// Gets the type of expression source.
    /// </summary>
    public NoCodeExpressionType Type { get; }

    /// <summary>
    /// Gets the 1-based argument index for Argument type expressions.
    /// </summary>
    public int? ArgumentIndex { get; }

    /// <summary>
    /// Gets the property path to navigate (e.g., ["Customer", "Email"] for $arg1.Customer.Email).
    /// </summary>
    public string[] PropertyPath { get; }

    /// <summary>
    /// Gets the original raw expression string.
    /// </summary>
    public string RawExpression { get; }

    /// <summary>
    /// Parses an expression string into a NoCodeExpression.
    /// </summary>
    /// <param name="expression">The expression string (e.g., "$arg1.Property").</param>
    /// <returns>A parsed NoCodeExpression, or null if the expression is invalid.</returns>
    public static NoCodeExpression? Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        var match = ExpressionPattern.Match(expression!.Trim());
        if (!match.Success)
        {
            Log.Debug("Invalid expression syntax: '{0}'", expression);
            return null;
        }

        var source = match.Groups["source"].Value;
        var indexStr = match.Groups["index"].Value;
        var pathStr = match.Groups["path"].Value;

        NoCodeExpressionType type;
        int? argumentIndex = null;

        if (source.StartsWith("arg", StringComparison.Ordinal))
        {
            type = NoCodeExpressionType.Argument;
            if (!int.TryParse(indexStr, out var idx) || idx < 1 || idx > 9)
            {
                Log.Debug("Invalid argument index in expression: '{0}'", expression);
                return null;
            }

            argumentIndex = idx;
        }
        else
        {
            type = source switch
            {
                "instance" => NoCodeExpressionType.Instance,
                "return" => NoCodeExpressionType.Return,
                "method" => NoCodeExpressionType.MethodName,
                "type" => NoCodeExpressionType.TypeName,
                _ => NoCodeExpressionType.Literal
            };
        }

        var propertyPath = string.IsNullOrEmpty(pathStr)
            ? Array.Empty<string>()
#if NET
            : pathStr.Split('.', StringSplitOptions.RemoveEmptyEntries);
#else
            : pathStr.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
#endif
        if (propertyPath.Length > MaxPropertyDepth)
        {
            Log.Debug("Property path exceeds maximum depth ({0}): '{1}'", MaxPropertyDepth, expression);
            return null;
        }

        return new NoCodeExpression(type, argumentIndex, propertyPath, expression);
    }

    /// <summary>
    /// Evaluates the expression against the provided context.
    /// </summary>
    /// <param name="context">The method execution context.</param>
    /// <returns>The evaluated value, or null if evaluation fails.</returns>
    internal object? Evaluate(NoCodeExpressionContext context)
    {
        object? target = Type switch
        {
            NoCodeExpressionType.Argument => GetArgumentValue(context),
            NoCodeExpressionType.Instance => context.Instance,
            NoCodeExpressionType.Return => context.ReturnValue,
            NoCodeExpressionType.MethodName => context.MethodName,
            NoCodeExpressionType.TypeName => context.TypeName,
            _ => null
        };

        // Navigate property path
        foreach (var propertyName in PropertyPath)
        {
            if (target == null)
            {
                return null;
            }

            target = GetPropertyValue(target, propertyName);
        }

        return target;
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        var targetType = target.GetType();
        var cacheKey = (targetType, propertyName);

        var property = PropertyCache.GetOrAdd(cacheKey, key =>
        {
            var prop = key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
            {
                Log.Debug("Property '{0}' not found on type '{1}'", key.Item2, key.Item1.FullName);
            }

            return prop;
        });

        if (property == null)
        {
            return null;
        }

        try
        {
            return property.GetValue(target);
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to get property '{0}' value: {1}", propertyName, ex.Message);
            return null;
        }
    }

    private object? GetArgumentValue(NoCodeExpressionContext context)
    {
        if (ArgumentIndex == null || context.Arguments == null)
        {
            return null;
        }

        var index = ArgumentIndex.Value - 1; // Convert to 0-based
        if (index < 0 || index >= context.Arguments.Length)
        {
            Log.Debug("Argument index {0} out of range (total arguments: {1})", ArgumentIndex.Value, context.Arguments.Length);
            return null;
        }

        return context.Arguments[index];
    }
}
