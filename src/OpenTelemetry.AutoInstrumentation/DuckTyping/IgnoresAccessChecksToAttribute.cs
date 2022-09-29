// <copyright file="IgnoresAccessChecksToAttribute.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// This attribute is recognized by the CLR and allow us to disable visibility checks for certain assemblies (only from 4.6+)
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class IgnoresAccessChecksToAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoresAccessChecksToAttribute"/> class.
    /// </summary>
    /// <param name="assemblyName">Assembly name</param>
    public IgnoresAccessChecksToAttribute(string assemblyName)
    {
        AssemblyName = assemblyName;
    }

    /// <summary>
    /// Gets the assembly name
    /// </summary>
    public string AssemblyName { get; }
}
