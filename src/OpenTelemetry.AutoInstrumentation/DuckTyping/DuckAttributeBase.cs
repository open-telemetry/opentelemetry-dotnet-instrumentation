// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck attribute
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
internal abstract class DuckAttributeBase : Attribute
{
    /// <summary>
    /// Gets or sets the underlying type member name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the binding flags
    /// </summary>
    public BindingFlags BindingFlags { get; set; } = DuckAttribute.DefaultFlags;

    /// <summary>
    /// Gets or sets the generic parameter type names definition for a generic method call (required when calling generic methods and instance type is non public)
    /// </summary>
    public string[]? GenericParameterTypeNames { get; set; }

    /// <summary>
    /// Gets or sets the parameter type names of the target method (optional / used to disambiguation)
    /// </summary>
    public string[]? ParameterTypeNames { get; set; }

    /// <summary>
    /// Gets or sets the explicit interface type name
    /// </summary>
    public string? ExplicitInterfaceTypeName { get; set; }
}
