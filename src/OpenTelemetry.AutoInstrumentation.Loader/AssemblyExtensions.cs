// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
#if !NET9_0_OR_GREATER

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// Provides SetEntryAssembly for .NET 8 via reflection.
/// On .NET 9+ the public API exists, so this is excluded.
/// Delete this file when .NET 8 support is dropped.
/// </summary>
internal static class AssemblyExtensions
{
    extension(Assembly)
    {
        public static void SetEntryAssembly(Assembly assembly)
        {
            SetEntryAssemblyDelegate?.Invoke(assembly);
        }
    }

    // Note: On .NET 8 SetEntryAssembly is non-public, but on .NET 9+ it becomes public.
    // We search for both NonPublic and Public because this code runs on all frameworks
    // until we stop building the Loader for .NET 8 only.
    // Note 2: Keep this field below extension method to satisfy StyleCop SA1201
    // that doesn't yet recognize C# 14 extension blocks after field declaration.
    private static readonly Action<Assembly>? SetEntryAssemblyDelegate =
        typeof(Assembly)
            .GetMethod("SetEntryAssembly", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, [typeof(Assembly)], null)
            ?.CreateDelegate<Action<Assembly>>();
}

#endif
#endif
