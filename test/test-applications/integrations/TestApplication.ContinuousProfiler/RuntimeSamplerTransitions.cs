// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
#if NET
using System.Reflection;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestApplication.ContinuousProfiler;

// This is an integration-test-only driver for the versioned native ABI. Production managed host selection and
// control-plane transport deliberately remain outside this test fixture and this PR.
internal static class RuntimeSamplerTransitions
{
    private const uint CpuSamplingIntervalMilliseconds = 500;
    private const int Applied = 0;
#if NET
    private const uint MaxAllocationSamplesPerMinute = 6000;
#endif

    private enum RuntimeSamplerAuthority : uint
    {
        ControlPlane = 2,
    }

    public static void Run()
    {
        Apply(CpuSamplingIntervalMilliseconds, GetAllocationSamplesPerMinute(), "enabled");
        CaptureBeforeDisable();

        Apply(0, 0, "disabled");
        CaptureWhileDisabled();

        Apply(CpuSamplingIntervalMilliseconds, GetAllocationSamplesPerMinute(), "re-enabled");
        CaptureAfterReenable();
    }

    private static void Apply(uint cpuSamplingIntervalMilliseconds, uint maxAllocationSamplesPerMinute, string phase)
    {
        var configuration = new RuntimeSamplerConfiguration
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerConfiguration>(),
            CpuSamplingIntervalMilliseconds = cpuSamplingIntervalMilliseconds,
            MaxAllocationSamplesPerMinute = maxAllocationSamplesPerMinute,
        };
        var state = new RuntimeSamplerState
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerState>(),
        };

        var result = RuntimeSamplerNative.Apply(ref configuration, (uint)RuntimeSamplerAuthority.ControlPlane, ref state);
        if (result != Applied ||
            state.Authority != (uint)RuntimeSamplerAuthority.ControlPlane ||
            state.CommittedConfiguration.CpuSamplingIntervalMilliseconds != cpuSamplingIntervalMilliseconds ||
            state.CommittedConfiguration.MaxAllocationSamplesPerMinute != maxAllocationSamplesPerMinute)
        {
            throw new InvalidOperationException($"Runtime sampler {phase} transition failed: {result}.");
        }

        Console.WriteLine($"Runtime sampler transition applied: {phase}.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CaptureBeforeDisable()
    {
        KeepCpuBusy();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CaptureWhileDisabled()
    {
        KeepCpuBusy();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CaptureAfterReenable()
    {
        KeepCpuBusy(allocate: true);
    }

    private static uint GetAllocationSamplesPerMinute()
    {
#if NET
        return MaxAllocationSamplesPerMinute;
#else
        return 0;
#endif
    }

    private static void KeepCpuBusy(bool allocate = false)
    {
        var deadline = Stopwatch.GetTimestamp() + (Stopwatch.Frequency * 2);
        var value = 0u;
        while (Stopwatch.GetTimestamp() < deadline)
        {
            value = (value * 1664525) + 1013904223;
            if (allocate && (value & 0x3fff) == 0)
            {
                GC.KeepAlive(new byte[1024]);
            }
        }

        GC.KeepAlive(value);
    }

    private static class RuntimeSamplerNative
    {
#if NET
        static RuntimeSamplerNative()
        {
            NativeLibrary.SetDllImportResolver(typeof(RuntimeSamplerNative).Assembly, ImportResolver);
        }
#endif

        public static int Apply(
            ref RuntimeSamplerConfiguration configuration,
            uint authority,
            ref RuntimeSamplerState state)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ApplyWindows(ref configuration, authority, ref state)
                : ApplyNonWindows(ref configuration, authority, ref state);
        }

#if NET
        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName is "OpenTelemetry.AutoInstrumentation.Native" or "OpenTelemetry.AutoInstrumentation.Native.dll")
            {
                return NativeLibrary.Load(GetProfilerPath());
            }

            return IntPtr.Zero;
        }

        private static string GetProfilerPath()
        {
            var bitnessSpecificPathVariable = Environment.Is64BitProcess
                ? "CORECLR_PROFILER_PATH_64"
                : "CORECLR_PROFILER_PATH_32";
            var profilerPath = Environment.GetEnvironmentVariable(bitnessSpecificPathVariable) ??
                Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH");

            if (!string.IsNullOrWhiteSpace(profilerPath))
            {
                return profilerPath;
            }

            throw new DllNotFoundException("Could not find native profiler path.");
        }
#endif

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", EntryPoint = "ApplyContinuousProfilerConfigurationV1")]
        private static extern int ApplyWindows(
            ref RuntimeSamplerConfiguration configuration,
            uint authority,
            ref RuntimeSamplerState state);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", EntryPoint = "ApplyContinuousProfilerConfigurationV1")]
        private static extern int ApplyNonWindows(
            ref RuntimeSamplerConfiguration configuration,
            uint authority,
            ref RuntimeSamplerState state);
    }
}
