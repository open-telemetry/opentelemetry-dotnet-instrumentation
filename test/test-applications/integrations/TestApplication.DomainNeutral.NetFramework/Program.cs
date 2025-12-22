// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace TestApplication.DomainNeutral.NetFramework;

using TestLibrary.InstrumentationTarget;

internal static class Program
{
    [LoaderOptimization(LoaderOptimization.MultiDomain)]
    public static void Main(string[] args)
    {
        var command = new Command();
        command.Execute();

        // Instrumentation assembly is expected to be already loaded from the GAC at this point.
        var instrumentationAssembly = Assembly.Load("OpenTelemetry.AutoInstrumentation") ?? throw new InvalidOperationException("Instrumentation assembly was not loaded.");

#if NETFRAMEWORK
        if (!instrumentationAssembly.GlobalAssemblyCache)
        {
            throw new InvalidOperationException("Instrumentation assembly was not loaded from the GAC");
        }
#endif
    }
}
