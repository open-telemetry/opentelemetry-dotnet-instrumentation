// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;
using TestApplication.ContinuousProfiler;

namespace TestApplication.SelectiveSampler;

internal static class RuntimeCpuSamplingScenario
{
    public static readonly SemaphoreSlim CpuBatches = new(0);
    public static readonly SemaphoreSlim SelectiveBatches = new(0);

    static RuntimeCpuSamplingScenario()
    {
#if NET
        NativeLibrary.SetDllImportResolver(typeof(RuntimeCpuSamplingScenario).Assembly, (name, _, _) =>
        {
            if (name != "OpenTelemetry.AutoInstrumentation.Native")
            {
                return nint.Zero;
            }

            var bitnessSpecificPath = Environment.Is64BitProcess ? "CORECLR_PROFILER_PATH_64" : "CORECLR_PROFILER_PATH_32";
            var path = Environment.GetEnvironmentVariable(bitnessSpecificPath) ??
                       Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH");
            return NativeLibrary.Load(path ?? throw new DllNotFoundException("Native profiler path is missing."));
        });
#endif
    }

    public static void Run(ActivitySource source)
    {
        using var activity = source.StartActivity("runtime-cpu-transitions");
        WaitForTwoBatches(SelectiveBatches);
        Apply(cpuInterval: 200);
        WaitForTwoBatches(CpuBatches);
        Apply(cpuInterval: 0);

        // Discard notifications from the CPU-enabled phase, then await fresh selective exports.
        while (SelectiveBatches.Wait(0))
        {
        }

        WaitForTwoBatches(SelectiveBatches);
    }

    private static void WaitForTwoBatches(SemaphoreSlim batches)
    {
        for (var i = 0; i < 2; i++)
        {
            if (!batches.Wait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Expected profiling batches were not exported.");
            }
        }
    }

    private static void Apply(uint cpuInterval)
    {
        const uint controlPlaneAuthority = 2;
        var configuration = new RuntimeSamplerConfiguration
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerConfiguration>(),
            CpuSamplingIntervalMilliseconds = cpuInterval,
            SelectiveThreadSamplingIntervalMilliseconds = 50,
        };
        var state = new RuntimeSamplerState
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerState>(),
        };
        var result = ApplyConfiguration(ref configuration, controlPlaneAuthority, ref state);
        if (result != 0 || state.Authority != controlPlaneAuthority ||
            state.CommittedConfiguration.CpuSamplingIntervalMilliseconds != cpuInterval ||
            state.CommittedConfiguration.SelectiveThreadSamplingIntervalMilliseconds != 50)
        {
            throw new InvalidOperationException($"Runtime CPU configuration failed: {result}.");
        }
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [DllImport("OpenTelemetry.AutoInstrumentation.Native", EntryPoint = "ApplyContinuousProfilerConfiguration", CallingConvention = CallingConvention.Winapi)]
    private static extern int ApplyConfiguration(ref RuntimeSamplerConfiguration configuration, uint authority, ref RuntimeSamplerState state);
}
