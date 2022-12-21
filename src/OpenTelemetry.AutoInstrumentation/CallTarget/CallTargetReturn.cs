// <copyright file="CallTargetReturn.cs" company="OpenTelemetry Authors">
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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Call target return value
/// </summary>
/// <typeparam name="T">Type of the return value</typeparam>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct CallTargetReturn<T>
{
    private readonly T _returnValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallTargetReturn{T}"/> struct.
    /// </summary>
    /// <param name="returnValue">Return value</param>
    public CallTargetReturn(T returnValue)
    {
        _returnValue = returnValue;
    }

    /// <summary>
    /// Gets the default call target return value (used by the native side to initialize the locals)
    /// </summary>
    /// <returns>Default call target return value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CallTargetReturn<T> GetDefault()
    {
        return default;
    }

    /// <summary>
    /// Gets the return value
    /// </summary>
    /// <returns>Return value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetReturnValue() => _returnValue;

    /// <summary>
    /// ToString override
    /// </summary>
    /// <returns>String value</returns>
    public override string ToString()
    {
        return $"{typeof(CallTargetReturn<T>).FullName}({_returnValue})";
    }
}

/// <summary>
/// Call target return value
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct CallTargetReturn
{
    /// <summary>
    /// Gets the default call target return value (used by the native side to initialize the locals)
    /// </summary>
    /// <returns>Default call target return value</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CallTargetReturn GetDefault()
    {
        return default;
    }
}
