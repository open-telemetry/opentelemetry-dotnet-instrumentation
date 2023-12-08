// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck kind
/// </summary>
internal enum DuckKind
{
    /// <summary>
    /// Property
    /// </summary>
    Property,

    /// <summary>
    /// Field
    /// </summary>
    Field
}

/// <summary>
/// Duck attribute
/// </summary>
internal class DuckAttribute : DuckAttributeBase
{
    /// <summary>
    /// Default BindingFlags
    /// </summary>
    public const BindingFlags DefaultFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy;

    /// <summary>
    /// Gets or sets duck kind
    /// </summary>
    public DuckKind Kind { get; set; } = DuckKind.Property;
}
