// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NET6_0_OR_GREATER

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Provides CreateDelegate&lt;T&gt; polyfill for frameworks before .NET 6.
/// On .NET 6+ the generic CreateDelegate exists, so this is excluded.
/// Delete this file when the project targets .NET6+.
/// </summary>
internal static class MethodInfoExtensions
{
    /// <summary>
    /// Creates a delegate of the specified type from this method.
    /// Polyfill for the generic CreateDelegate&lt;T&gt; that doesn't exist in netcoreapp3.1.
    /// </summary>
    public static T? CreateDelegate<T>(this MethodInfo methodInfo)
        where T : Delegate
    {
        return (T?)methodInfo.CreateDelegate(typeof(T));
    }
}

#endif
