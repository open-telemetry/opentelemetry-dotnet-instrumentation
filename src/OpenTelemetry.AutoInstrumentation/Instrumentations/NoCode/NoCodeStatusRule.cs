// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Represents a rule for setting span status based on a CEL condition expression.
/// </summary>
internal class NoCodeStatusRule
{
    public NoCodeStatusRule(CelExpression condition, ActivityStatusCode statusCode, string? description)
    {
        Condition = condition;
        StatusCode = statusCode;
        Description = description;
    }

    /// <summary>
    /// Gets the CEL condition expression.
    /// The condition should evaluate to a boolean value.
    /// </summary>
    public CelExpression Condition { get; }

    /// <summary>
    /// Gets the status code to set when the condition is true.
    /// </summary>
    public ActivityStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the optional description for the status (often used with Error status).
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Evaluates the condition against the provided context.
    /// </summary>
    /// <param name="context">The method execution context.</param>
    /// <returns>True if the condition is met, false otherwise.</returns>
    public bool EvaluateCondition(NoCodeExpressionContext context)
    {
        object? result = Condition.Evaluate(context);

        // Convert result to boolean
        return result switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s) && !s.Equals("false", StringComparison.OrdinalIgnoreCase),
            int i => i != 0,
            long l => l != 0,
            null => false,
            _ => true
        };
    }
}
