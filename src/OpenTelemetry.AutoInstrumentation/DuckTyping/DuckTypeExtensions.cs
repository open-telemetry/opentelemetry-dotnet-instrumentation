// <copyright file="DuckTypeExtensions.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.DuckTyping;

/// <summary>
/// Duck type extensions
/// </summary>
internal static class DuckTypeExtensions
{
    /// <summary>
    /// Gets the duck type instance for the object implementing a base class or interface T
    /// </summary>
    /// <param name="instance">Object instance</param>
    /// <typeparam name="T">Target type</typeparam>
    /// <returns>DuckType instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull("instance")]
    public static T? DuckCast<T>(this object? instance)
        => DuckType.Create<T>(instance);

    /// <summary>
    /// Gets the duck type instance for the object implementing a base class or interface T
    /// </summary>
    /// <param name="instance">Object instance</param>
    /// <param name="targetType">Target type</param>
    /// <returns>DuckType instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object DuckCast(this object instance, Type targetType)
        => DuckType.Create(targetType, instance);

    /// <summary>
    /// Tries to ducktype the object implementing a base class or interface T
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="instance">Object instance</param>
    /// <param name="value">Ducktype instance</param>
    /// <returns>true if the object instance was ducktyped; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDuckCast<T>(this object? instance, [NotNullWhen(true)] out T? value)
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        DuckType.CreateTypeResult proxyResult = DuckType.CreateCache<T>.GetProxy(instance.GetType());
        if (proxyResult.Success)
        {
            value = proxyResult.CreateInstance<T>(instance)!;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Tries to ducktype the object implementing a base class or interface T
    /// </summary>
    /// <param name="instance">Object instance</param>
    /// <param name="targetType">Target type</param>
    /// <param name="value">Ducktype instance</param>
    /// <returns>true if the object instance was ducktyped; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDuckCast(this object? instance, Type? targetType, [NotNullWhen(true)] out object? value)
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        if (targetType != null)
        {
            DuckType.CreateTypeResult proxyResult = DuckType.GetOrCreateProxyType(targetType, instance.GetType());
            if (proxyResult.Success)
            {
                value = proxyResult.CreateInstance(instance);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Gets the duck type instance for the object implementing a base class or interface T
    /// </summary>
    /// <param name="instance">Object instance</param>
    /// <typeparam name="T">Target type</typeparam>
    /// <returns>DuckType instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? DuckAs<T>(this object? instance)
        where T : class
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        DuckType.CreateTypeResult proxyResult = DuckType.CreateCache<T>.GetProxy(instance.GetType());
        if (proxyResult.Success)
        {
            return proxyResult.CreateInstance<T>(instance);
        }

        return null;
    }

    /// <summary>
    /// Gets the duck type instance for the object implementing a base class or interface T
    /// </summary>
    /// <param name="instance">Object instance</param>
    /// <param name="targetType">Target type</param>
    /// <returns>DuckType instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? DuckAs(this object? instance, Type? targetType)
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        if (targetType != null)
        {
            DuckType.CreateTypeResult proxyResult = DuckType.GetOrCreateProxyType(targetType, instance.GetType());
            if (proxyResult.Success)
            {
                return proxyResult.CreateInstance(instance);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets if a proxy can be created
    /// </summary>
    /// <param name="instance">Instance object</param>
    /// <typeparam name="T">Duck type</typeparam>
    /// <returns>true if the proxy can be created; otherwise, false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DuckIs<T>(this object? instance)
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        return DuckType.CanCreate<T>(instance);
    }

    /// <summary>
    /// Gets if a proxy can be created
    /// </summary>
    /// <param name="instance">Instance object</param>
    /// <param name="targetType">Duck type</param>
    /// <returns>true if the proxy can be created; otherwise, false</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DuckIs(this object? instance, Type? targetType)
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        if (targetType != null)
        {
            return DuckType.CanCreate(targetType, instance);
        }

        return false;
    }

    /// <summary>
    /// Gets or creates a proxy that implements/derives from <paramref name="typeToDeriveFrom"/>,
    /// and delegates implementations/overrides to <paramref name="instance"/>
    /// </summary>
    /// <param name="instance">The instance containing additional overrides/implementations</param>
    /// <param name="typeToDeriveFrom">The type to derive from</param>
    /// <returns>DuckType instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object DuckImplement(this object instance, Type typeToDeriveFrom)
        => DuckType.CreateReverse(typeToDeriveFrom, instance);

    /// <summary>
    /// Tries to create a proxy that implements/derives from <paramref name="typeToDeriveFrom"/>,
    /// and delegates implementations/overrides to <paramref name="instance"/>
    /// ducktype the object implementing a base class or interface T
    /// </summary>
    /// <param name="instance">The instance containing additional overrides/implementations</param>
    /// <param name="typeToDeriveFrom">The type to derive from</param>
    /// <param name="value">The Ducktype instance</param>
    /// <returns>true if the object instance was ducktyped; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDuckImplement(this object? instance, Type? typeToDeriveFrom, [NotNullWhen(true)] out object? value)
    {
        if (instance is null)
        {
            DuckTypeTargetObjectInstanceIsNull.Throw();
        }

        if (typeToDeriveFrom != null)
        {
            DuckType.CreateTypeResult proxyResult = DuckType.GetOrCreateReverseProxyType(typeToDeriveFrom, instance.GetType());
            if (proxyResult.Success)
            {
                value = proxyResult.CreateInstance(instance);
                return true;
            }
        }

        value = default;
        return false;
    }
}
