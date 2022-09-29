// <copyright file="DuckType.Utilities.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck Type
/// </summary>
public static partial class DuckType
{
    /// <summary>
    /// Checks and ensures the arguments for the Create methods
    /// </summary>
    /// <param name="proxyType">Duck type</param>
    /// <param name="instance">Instance value</param>
    /// <exception cref="ArgumentNullException">If the duck type or the instance value is null</exception>
    private static void EnsureArguments(Type proxyType, object instance)
    {
        if (proxyType is null)
        {
            DuckTypeProxyTypeDefinitionIsNull.Throw();
        }

        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }
    }

    /// <summary>
    /// Ensures the visibility access to the type
    /// </summary>
    /// <param name="builder">Module builder</param>
    /// <param name="type">Type to gain internals visibility</param>
    private static void EnsureTypeVisibility(ModuleBuilder builder, Type type)
    {
        EnsureAssemblyNameVisibility(builder, type.Assembly.GetName().Name);

        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            foreach (Type t in type.GetGenericArguments())
            {
                if (!t.IsVisible)
                {
                    EnsureAssemblyNameVisibility(builder, t.Assembly.GetName().Name);
                }
            }
        }

        while (type.IsNested)
        {
            if (!type.IsNestedPublic)
            {
                EnsureAssemblyNameVisibility(builder, type.Assembly.GetName().Name);
            }

            // this should be null for non-nested types.
            type = type.DeclaringType;
        }

        static void EnsureAssemblyNameVisibility(ModuleBuilder builder, string assemblyName)
        {
            lock (_ignoresAccessChecksToAssembliesSetDictionary)
            {
                if (!_ignoresAccessChecksToAssembliesSetDictionary.TryGetValue(builder, out var hashSet))
                {
                    hashSet = new HashSet<string>();
                    _ignoresAccessChecksToAssembliesSetDictionary[builder] = hashSet;
                }

                if (hashSet.Add(assemblyName))
                {
                    ((AssemblyBuilder)builder.Assembly).SetCustomAttribute(new CustomAttributeBuilder(_ignoresAccessChecksToAttributeCtor, new object[] { assemblyName }));
                }
            }
        }
    }

    private static bool NeedsDuckChaining(Type targetType, Type proxyType)
    {
        // The condition to apply duck chaining is:
        // 1. Is a struct with the DuckCopy attribute
        // 2. Both types must be differents.
        // 3. The proxy type (duck chaining proxy definition type) can't be a struct
        // 4. The proxy type can't be a generic parameter (should be a well known type)
        // 5. Can't be a base type or an iterface implemented by the targetType type.
        // 6. The proxy type can't be a CLR type
        return proxyType.GetCustomAttribute<DuckCopyAttribute>() != null ||
               (proxyType != targetType &&
                !proxyType.IsValueType &&
                !proxyType.IsGenericParameter &&
                !proxyType.IsAssignableFrom(targetType) &&
                proxyType.Module != typeof(string).Module);
    }

    /// <summary>
    /// Gets if the direct access method should be used or the inderect method (dynamic method)
    /// </summary>
    /// <param name="builder">Module builder</param>
    /// <param name="targetType">Target type</param>
    /// <returns>true for direct method; otherwise, false.</returns>
    private static bool UseDirectAccessTo(ModuleBuilder builder, Type targetType)
    {
        if (builder == null)
        {
            return targetType.IsPublic || targetType.IsNestedPublic;
        }

        EnsureTypeVisibility(builder, targetType);
        return true;
    }

    /// <summary>
    /// Gets if the direct access method should be used or the inderect method (dynamic method)
    /// </summary>
    /// <param name="builder">Type builder</param>
    /// <param name="targetType">Target type</param>
    /// <returns>true for direct method; otherwise, false.</returns>
    private static bool UseDirectAccessTo(TypeBuilder builder, Type targetType)
    {
        return UseDirectAccessTo((ModuleBuilder)builder?.Module, targetType);
    }

    /// <summary>
    /// Gets if the direct access method should be used or the inderect method (dynamic method)
    /// </summary>
    /// <param name="targetType">Target type</param>
    /// <returns>true for direct method; otherwise, false.</returns>
    private static bool UseDirectAccessTo(Type targetType)
    {
        return UseDirectAccessTo((ModuleBuilder)null, targetType);
    }
}
