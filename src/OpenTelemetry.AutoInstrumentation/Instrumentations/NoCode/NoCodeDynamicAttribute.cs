// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Represents a dynamic attribute that extracts its value from method context using a CEL expression.
/// </summary>
internal class NoCodeDynamicAttribute
{
    public NoCodeDynamicAttribute(string name, CelExpression expression, string type)
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
    /// Gets the CEL expression used to extract the attribute value.
    /// </summary>
    public CelExpression Expression { get; }

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
        return Expression.Evaluate(context);
    }
}
