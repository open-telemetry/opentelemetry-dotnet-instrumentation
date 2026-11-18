// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using OpenTelemetry.AutoInstrumentation.PluginApi;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// Clears the profiler environment flag after the native profiler has bootstrapped the Loader.
/// </summary>
public sealed class ProfilerEnvironmentPlugin : IPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void Initializing()
    {
        Environment.SetEnvironmentVariable("COR_ENABLE_PROFILING", null);
        Console.WriteLine("COR_ENABLE_PROFILING cleared after native bootstrap.");
    }

    public void Initialized()
    {
    }
}
#endif
