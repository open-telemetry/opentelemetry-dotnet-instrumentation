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

        // OpAMP tests close stdin after verifying the background report reached the server.
        // Console reads can block synchronously, so run the read separately to bound the wait.
        if (!Task.Run(Console.ReadLine).Wait(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("The test did not release the application.");
        }
    }
}
