// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !NET9_0_OR_GREATER

using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Util;

/// <summary>
/// Provides SetEntryAssembly for frameworks before .NET 9.
/// On .NET 9+ the public API exists, so this is excluded.
/// Delete this file when the project targets .NET9+.
/// </summary>
internal static class AssemblyExtensions
{
    // Note: Before .NET 9 SetEntryAssembly was non-public but we search for both
    // NonPublic and Public because this project still compiles only for targets
    // lower than .NET 9 so it will be executed even on later runtimes.
    private static readonly Action<Assembly>? SetEntryAssemblyDelegate =
        typeof(Assembly)
            .GetMethod("SetEntryAssembly", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, [typeof(Assembly)], null)
            ?.CreateDelegate<Action<Assembly>>();

    extension(Assembly)
    {
        public static void SetEntryAssembly(Assembly assembly)
        {
            SetEntryAssemblyDelegate?.Invoke(assembly);
        }
    }
}

#endif
