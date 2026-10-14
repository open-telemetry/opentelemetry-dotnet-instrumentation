// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1812", Justification = "The linked smoke application's static entry point is invoked by OpAmpProgram.", Scope = "type", Target = "~T:TestApplication.Smoke.Program")]

namespace TestApplication.Plugins;

internal static class OpAmpProgram
{
    public static void Main(string[] args)
    {
        Smoke.Program.Main(args);
        if (args.Length == 0 || args[0] != "--wait-for-release")
        {
            return;
        }

        // The parent test closes stdin after verifying delivery and bounds process shutdown.
        Console.ReadLine();
    }
}
