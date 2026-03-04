// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// CEL (Common Expression Language) compatible expression evaluator.
/// Supports a minimal subset of CEL for performance-critical instrumentation scenarios.
/// </summary>
/// <remarks>
/// Supported features:
/// - Member access: instance.property, arguments[0].field
/// - Operators: ==, !=, &gt;, &lt;, &gt;=, &lt;=, &amp;&amp;, ||, !
/// - Functions: concat(), coalesce(), substring(), string(), size()
/// - Literals: strings ("value", 'value'), numbers (123, 123.45), booleans (true, false), null
/// - Ternary: condition ? trueValue : falseValue
/// </remarks>
internal sealed class CelExpression
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();
    private static readonly ConcurrentDictionary<(Type, string), Func<object, object?>?> PropertyGetterCache = new();

    private readonly CelNode _root;
    private readonly string _rawExpression;

    private CelExpression(CelNode root, string rawExpression)
    {
        _root = root;
        _rawExpression = rawExpression;
    }

    /// <summary>
    /// Gets the original raw expression string.
    /// </summary>
    public string RawExpression => _rawExpression;

    /// <summary>
    /// Parses a CEL expression string.
    /// </summary>
    /// <param name="expression">The CEL expression string.</param>
    /// <returns>A parsed CelExpression, or null if the expression is invalid.</returns>
    public static CelExpression? Parse(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        try
        {
            var tokens = CelLexer.Tokenize(expression!);
            var parser = new CelParser(tokens);
            var root = parser.Parse();
            return new CelExpression(root, expression!);
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to parse CEL expression '{0}': {1}", expression, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Evaluates the CEL expression against the provided context.
    /// </summary>
    /// <param name="context">The method execution context.</param>
    /// <returns>The evaluated value, or null if evaluation fails.</returns>
    public object? Evaluate(NoCodeExpressionContext context)
    {
        try
        {
            return _root.Evaluate(context);
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to evaluate CEL expression '{0}': {1}", _rawExpression, ex.Message);
            return null;
        }
    }

    internal static object? GetPropertyValue(object target, string propertyName)
    {
        var targetType = target.GetType();
        var cacheKey = (targetType, propertyName);

        var propertyGetter = PropertyGetterCache.GetOrAdd(cacheKey, key =>
        {
            var prop = key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null)
            {
                Log.Debug("Property '{0}' not found on type '{1}'", key.Item2, key.Item1.FullName);
                return null;
            }

            var getMethod = prop.GetGetMethod();
            if (getMethod == null)
            {
                // If the derived class overrides only the setter, try to find the getter in the base class
                var baseType = key.Item1.BaseType;
                while (baseType != null && getMethod == null)
                {
                    var baseProp = baseType.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (baseProp != null)
                    {
                        getMethod = baseProp.GetGetMethod();
                        if (getMethod != null)
                        {
                            break;
                        }
                    }

                    baseType = baseType.BaseType;
                }

                if (getMethod == null)
                {
                    Log.Debug("Property '{0}' on type '{1}' has no getter", key.Item2, key.Item1.FullName);
                    return null;
                }
            }

            // Verify the getter has no parameters (i.e., it's not an indexer)
            if (getMethod.GetParameters().Length > 0)
            {
                Log.Debug("Property '{0}' on type '{1}' is an indexer and cannot be accessed without parameters", key.Item2, key.Item1.FullName);
                return null;
            }

            return (Func<object, object?>)(obj => getMethod.Invoke(obj, null));
        });

        if (propertyGetter == null)
        {
            return null;
        }

        try
        {
            return propertyGetter(target);
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to get property '{0}' value: {1}", propertyName, ex.Message);
            return null;
        }
    }

    internal static object? GetIndexValue(object target, int index)
    {
        try
        {
            if (target is Array array)
            {
                if (index < 0 || index >= array.Length)
                {
                    return null;
                }

                return array.GetValue(index);
            }

            if (target is System.Collections.IList list)
            {
                if (index < 0 || index >= list.Count)
                {
                    return null;
                }

                return list[index];
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to get index '{0}' value: {1}", index, ex.Message);
            return null;
        }
    }
}
