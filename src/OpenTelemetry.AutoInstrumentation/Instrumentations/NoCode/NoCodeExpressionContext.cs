// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Context for evaluating NoCode expressions.
/// </summary>
internal readonly struct NoCodeExpressionContext
{
    public NoCodeExpressionContext(
        object? instance,
        object?[]? arguments,
        object? returnValue,
        string? methodName,
        string? typeName)
    {
        Instance = instance;
        Arguments = arguments;
        ReturnValue = returnValue;
        MethodName = methodName;
        TypeName = typeName;
    }

    /// <summary>
    /// Gets the instance object (null for static methods).
    /// </summary>
    public object? Instance { get; }

    /// <summary>
    /// Gets the method arguments.
    /// </summary>
    public object?[]? Arguments { get; }

    /// <summary>
    /// Gets the return value (only available in OnMethodEnd).
    /// </summary>
    public object? ReturnValue { get; }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// Gets the declaring type name.
    /// </summary>
    public string? TypeName { get; }
}
