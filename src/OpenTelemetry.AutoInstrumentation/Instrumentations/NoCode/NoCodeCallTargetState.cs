// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Holds context information needed between OnMethodBegin and OnMethodEnd.
/// </summary>
internal sealed class NoCodeCallTargetState
{
    public NoCodeCallTargetState(
        NoCodeInstrumentedMethod entry,
        object? instance,
        object?[]? arguments,
        string? methodName,
        string? typeName)
    {
        Entry = entry;
        Instance = instance;
        Arguments = arguments;
        MethodName = methodName;
        TypeName = typeName;
    }

    /// <summary>
    /// Gets the NoCode instrumented method entry.
    /// </summary>
    public NoCodeInstrumentedMethod Entry { get; }

    /// <summary>
    /// Gets the instance object.
    /// </summary>
    public object? Instance { get; }

    /// <summary>
    /// Gets the method arguments.
    /// </summary>
    public object?[]? Arguments { get; }

    /// <summary>
    /// Gets the method name.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// Gets the type name.
    /// </summary>
    public string? TypeName { get; }
}
