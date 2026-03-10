// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

/// <summary>
/// Helper utilities for CEL expression evaluation.
/// </summary>
internal static class CelHelpers
{
    /// <summary>
    /// Determines if a value should be considered "true" in a boolean context.
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>True if the value is considered truthy, false otherwise.</returns>
    public static bool IsTrue(object? value)
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
}
