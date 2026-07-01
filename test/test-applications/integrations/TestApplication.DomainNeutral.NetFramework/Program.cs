// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using TestApplication.Shared;

namespace TestApplication.DomainNeutral.NetFramework;

using TestLibrary.InstrumentationTarget;

internal static class Program
{
    [LoaderOptimization(LoaderOptimization.MultiDomain)]
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        var command = new Command();
        command.Execute();

        // The instrumented call above should have already loaded the instrumentation assembly. Do not call
        // Assembly.Load here, because that would test the current probing/GAC setup instead of the
        // assembly load path used by the instrumentation.
        var instrumentationAssemblyLoaded = AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly => string.Equals(
                assembly.GetName().Name,
                "OpenTelemetry.AutoInstrumentation",
                StringComparison.Ordinal));

        if (!instrumentationAssemblyLoaded)
        {
            throw new InvalidOperationException("Instrumentation assembly was not loaded.");
        }
    }
}
