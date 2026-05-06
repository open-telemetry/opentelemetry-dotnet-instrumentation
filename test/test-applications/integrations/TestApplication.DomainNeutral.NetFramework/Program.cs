// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
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

        // Direct CallTarget mode needs the instrumentation assembly to be loadable from the GAC for this
        // domain-neutral scenario. Trampoline mode intentionally avoids that direct target-assembly closure.
        var instrumentationAssembly = Assembly.Load("OpenTelemetry.AutoInstrumentation") ?? throw new InvalidOperationException("Instrumentation assembly was not loaded.");
        var callTargetTrampolineEnabled = string.Equals(
            Environment.GetEnvironmentVariable("OTEL_DOTNET_AUTO_CALLTARGET_TRAMPOLINE_ENABLED"),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

#if NETFRAMEWORK
        if (!callTargetTrampolineEnabled && !instrumentationAssembly.GlobalAssemblyCache)
        {
            throw new InvalidOperationException("Instrumentation assembly was not loaded from the GAC");
        }
#endif
    }
}
