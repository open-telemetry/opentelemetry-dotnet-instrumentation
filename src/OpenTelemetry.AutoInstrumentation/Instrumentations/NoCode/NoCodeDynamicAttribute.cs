// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Represents a dynamic attribute that extracts its value from method context using an expression.
/// </summary>
internal class NoCodeDynamicAttribute
{
    public NoCodeDynamicAttribute(string name, object expression, string type)
    {
        Name = name;
        Expression = expression;
        Type = type;
    }

    /// <summary>
    /// Gets the attribute name (tag key).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the expression used to extract the attribute value.
    /// Can be NoCodeExpression or NoCodeFunctionExpression.
    /// </summary>
    public object Expression { get; }

    /// <summary>
    /// Gets the expected type of the attribute value (string, int, double, bool, etc.).
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Evaluates the expression against the provided context.
    /// </summary>
    /// <param name="context">The method execution context.</param>
    /// <returns>The evaluated value, or null if evaluation fails.</returns>
    public object? Evaluate(NoCodeExpressionContext context)
    {
        return Expression switch
        {
            NoCodeExpression expr => expr.Evaluate(context),
            NoCodeFunctionExpression func => func.Evaluate(context),
            _ => null
        };
    }
}
