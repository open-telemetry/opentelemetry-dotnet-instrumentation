// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class ManagedProfilerAssemblyLoadContext : AssemblyLoadContext
{
    // TODO temporary colored console output for debugging purpose
    public ManagedProfilerAssemblyLoadContext(string? name = null, bool isCollectible = false)
        : base(name ?? "OpenTelemetry.AutoInstrumentation.Loader", isCollectible)
    {
        if (Environment.GetEnvironmentVariable("4715_DEBUG_TRACES") is not null)
        {
            Resolving += (context, assemblyName) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"Resolving <{assemblyName}>@({context}): SKIP");
                Console.ResetColor();
                return null;
            };
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (Environment.GetEnvironmentVariable("4715_DEBUG_TRACES") is not null)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Loading <{assemblyName}>@({this}): SKIP");
            Console.ResetColor();
        }

        return null;
    }
}
#endif
