// <copyright file="DuckAttributeBase.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#nullable enable

using System;
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
