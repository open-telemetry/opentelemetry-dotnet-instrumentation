// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// Represents the type of expression source.
/// </summary>
internal enum NoCodeExpressionType
{
    /// <summary>
    /// Expression refers to a method argument ($arg1, $arg2, etc.).
    /// </summary>
    Argument,

    /// <summary>
    /// Expression refers to the instance object ($instance).
    /// </summary>
    Instance,

    /// <summary>
    /// Expression refers to the return value ($return).
    /// </summary>
    Return,

    /// <summary>
    /// Expression refers to the method name ($method).
    /// </summary>
    MethodName,

    /// <summary>
    /// Expression refers to the declaring type name ($type).
    /// </summary>
    TypeName,

    /// <summary>
    /// Expression is a literal value.
    /// </summary>
    Literal
}
