// <copyright file="DuckReverseMethodAttribute.cs" company="OpenTelemetry Authors">
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
/// Duck reverse method attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DuckReverseMethodAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuckReverseMethodAttribute"/> class.
    /// </summary>
    public DuckReverseMethodAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuckReverseMethodAttribute"/> class.
    /// </summary>
    /// <param name="arguments">Methods arguments</param>
    public DuckReverseMethodAttribute(params string[] arguments)
    {
        Arguments = arguments;
    }

    /// <summary>
    /// Gets the methods arguments
    /// </summary>
    public string[] Arguments { get; private set; }
}
