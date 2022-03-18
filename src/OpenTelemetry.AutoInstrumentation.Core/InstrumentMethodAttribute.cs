// <copyright file="InstrumentMethodAttribute.cs" company="OpenTelemetry Authors">
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

using System;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// Attribute that indicates that the decorated class is meant to intercept a method
/// by modifying the method body with callbacks. Used to generate the integration definitions file.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public class InstrumentMethodAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the assembly that contains the target method to be intercepted.
    /// Required if <see cref="AssemblyNames"/> is not set.
    /// </summary>
    public string AssemblyName
    {
        get
        {
            switch (AssemblyNames?.Length ?? 0)
            {
                case 0:
                    return null;
                case 1:
                    return AssemblyNames[0];
                default:
                    throw new NotSupportedException("Multiple assemblies are not supported using this property. Use AssemblyNames property instead.");
            }
        }
        set => AssemblyNames = new[] { value };
    }

    /// <summary>
    /// Gets or sets the name of the assemblies that contain the target method to be intercepted.
    /// Required if <see cref="AssemblyName"/> is not set.
    /// </summary>
    public string[] AssemblyNames { get; set; }

    /// <summary>
    /// Gets or sets the name of the type that contains the target method to be intercepted.
    /// Required.
    /// </summary>
    public string TypeName { get; set; }

    /// <summary>
    /// Gets or sets the name of the target method to be intercepted.
    /// If null, default to the name of the decorated method.
    /// </summary>
    public string MethodName { get; set; }

    /// <summary>
    /// Gets or sets the return type name
    /// </summary>
    public string ReturnTypeName { get; set; }

    /// <summary>
    /// Gets or sets the parameters type array for the target method to be intercepted.
    /// </summary>
    public string[] ParameterTypeNames { get; set; }

    /// <summary>
    /// Gets the target version range for <see cref="AssemblyName"/>.
    /// </summary>
    public IntegrationVersionRange VersionRange { get; } = new IntegrationVersionRange();

    /// <summary>
    /// Gets or sets the target minimum version.
    /// </summary>
    public string MinimumVersion
    {
        get => VersionRange.MinimumVersion;
        set => VersionRange.MinimumVersion = value;
    }

    /// <summary>
    /// Gets or sets the target maximum version.
    /// </summary>
    public string MaximumVersion
    {
        get => VersionRange.MaximumVersion;
        set => VersionRange.MaximumVersion = value;
    }

    /// <summary>
    /// Gets or sets the integration name. Allows to group several integration with a single integration name.
    /// </summary>
    public string IntegrationName { get; set; }

    /// <summary>
    /// Gets or sets the CallTarget Class used to instrument the method
    /// </summary>
    public Type CallTargetType { get; set; }
}
