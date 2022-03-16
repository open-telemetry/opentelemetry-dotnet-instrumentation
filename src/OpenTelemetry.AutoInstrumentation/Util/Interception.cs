// <copyright file="Interception.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Convenience properties and methods for integration definitions.
/// </summary>
internal static class Interception
{
    internal const Type[] NullTypeArray = null;
    internal static readonly object[] NoArgObjects = Array.Empty<object>();
    internal static readonly Type[] NoArgTypes = Type.EmptyTypes;
    internal static readonly Type VoidType = typeof(void);

    internal static Type[] ParamsToTypes(params object[] objectsToCheck)
    {
        var types = new Type[objectsToCheck.Length];

        for (var i = 0; i < objectsToCheck.Length; i++)
        {
            types[i] = objectsToCheck[i]?.GetType();
        }

        return types;
    }

    internal static string MethodKey(
        Type owningType,
        Type returnType,
        Type[] genericTypes,
        Type[] parameterTypes)
    {
        var key = $"{owningType?.AssemblyQualifiedName}_m_r{returnType?.AssemblyQualifiedName}";

        for (ushort i = 0; i < (genericTypes?.Length ?? 0); i++)
        {
            Debug.Assert(genericTypes != null, nameof(genericTypes) + " != null");
            key = string.Concat(key, $"_g{genericTypes[i].AssemblyQualifiedName}");
        }

        for (ushort i = 0; i < (parameterTypes?.Length ?? 0); i++)
        {
            Debug.Assert(parameterTypes != null, nameof(parameterTypes) + " != null");
            key = string.Concat(key, $"_p{parameterTypes[i].AssemblyQualifiedName}");
        }

        return key;
    }
}
