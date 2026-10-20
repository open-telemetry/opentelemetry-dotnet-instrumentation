// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
#if NET
using System.Reflection;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestApplication.ContinuousProfiler;

// This integration-test-only driver applies ControlPlane configuration directly through the native ABI.
// It simulates remote configuration at the contract boundary, independently of OpAMP transport.
internal static class RuntimeSamplerTransitions
{
    private const uint InitialCpuSamplingIntervalMilliseconds = 500;
    private const uint ReenabledCpuSamplingIntervalMilliseconds = 1000;
    private const int Applied = 0;
    private const int QuerySucceeded = 0;
    private const int RejectedInvalidConfiguration = 6;
    private const uint SeedAuthority = 1;
#if NET
    private const uint MaxAllocationSamplesPerMinute = 6000;
#endif

    private enum RuntimeSamplerAuthority : uint
    {
        ControlPlane = 2,
    }

    public static void Run()
    {
        VerifyInitialSeed();
        Apply(InitialCpuSamplingIntervalMilliseconds, GetAllocationSamplesPerMinute(), "enabled");
        VerifyInvalidControlPlaneUpdatePreservesState();
        CaptureBeforeDisable();

        Apply(0, 0, "disabled");
        CaptureWhileDisabled();

        Apply(ReenabledCpuSamplingIntervalMilliseconds, GetAllocationSamplesPerMinute(), "re-enabled");
        CaptureAfterReenable();
    }

    private static void VerifyInitialSeed()
    {
        var state = new RuntimeSamplerState
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerState>(),
        };

        var result = RuntimeSamplerNative.GetState(ref state);
        const uint expectedCpuSamplingIntervalMilliseconds = 0;
        const string expectedState = "disabled";
        if (result != QuerySucceeded ||
            state.Authority != SeedAuthority ||
            state.CommittedConfiguration.CpuSamplingIntervalMilliseconds != expectedCpuSamplingIntervalMilliseconds ||
            state.CommittedConfiguration.SelectiveThreadSamplingIntervalMilliseconds != 0 ||
            state.CommittedConfiguration.MaxAllocationSamplesPerMinute != 0)
        {
            throw new InvalidOperationException($"Unexpected initial runtime sampler Seed state: {result}.");
        }

        Console.WriteLine($"Runtime sampler initial Seed verified: {expectedState}.");
    }

    private static void Apply(
        uint cpuSamplingIntervalMilliseconds,
        uint maxAllocationSamplesPerMinute,
        string phase)
    {
        var configuration = new RuntimeSamplerConfiguration
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerConfiguration>(),
            CpuSamplingIntervalMilliseconds = cpuSamplingIntervalMilliseconds,
            SelectiveThreadSamplingIntervalMilliseconds = 0,
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
            state.CommittedConfiguration.SelectiveThreadSamplingIntervalMilliseconds != 0 ||
            state.CommittedConfiguration.MaxAllocationSamplesPerMinute != maxAllocationSamplesPerMinute)
        {
            throw new InvalidOperationException($"Runtime sampler {phase} transition failed: {result}.");
        }

        Console.WriteLine($"Runtime sampler transition applied: {phase}.");
    }

    private static void VerifyInvalidControlPlaneUpdatePreservesState()
    {
        var invalidConfiguration = new RuntimeSamplerConfiguration
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerConfiguration>(),
            CpuSamplingIntervalMilliseconds = InitialCpuSamplingIntervalMilliseconds,
            SelectiveThreadSamplingIntervalMilliseconds = 30,
            MaxAllocationSamplesPerMinute = GetAllocationSamplesPerMinute(),
        };
        var state = new RuntimeSamplerState
        {
            StructureSize = (uint)Marshal.SizeOf<RuntimeSamplerState>(),
        };

        var result = RuntimeSamplerNative.Apply(ref invalidConfiguration, (uint)RuntimeSamplerAuthority.ControlPlane, ref state);
        if (result != RejectedInvalidConfiguration ||
            state.Authority != (uint)RuntimeSamplerAuthority.ControlPlane ||
            state.CommittedConfiguration.CpuSamplingIntervalMilliseconds != InitialCpuSamplingIntervalMilliseconds ||
            state.CommittedConfiguration.SelectiveThreadSamplingIntervalMilliseconds != 0 ||
            state.CommittedConfiguration.MaxAllocationSamplesPerMinute != GetAllocationSamplesPerMinute())
        {
            throw new InvalidOperationException($"Invalid ControlPlane update changed the last-known-good runtime sampler state: {result}.");
        }

        Console.WriteLine("Runtime sampler invalid ControlPlane update rejected; last-known-good state preserved.");
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
        // Leave enough room for a full 1-second CPU cohort on slower CI workers.
        KeepCpuBusy(allocate: true, durationSeconds: 3);
    }

    private static uint GetAllocationSamplesPerMinute()
    {
#if NET
        return MaxAllocationSamplesPerMinute;
#else
        return 0;
#endif
    }

    private static void KeepCpuBusy(bool allocate = false, int durationSeconds = 2)
    {
        var deadline = Stopwatch.GetTimestamp() + (Stopwatch.Frequency * durationSeconds);
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

        public static int GetState(ref RuntimeSamplerState state)
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetStateWindows(ref state)
                : GetStateNonWindows(ref state);
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
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", EntryPoint = "ApplyContinuousProfilerConfiguration", CallingConvention = CallingConvention.Winapi)]
        private static extern int ApplyWindows(
            ref RuntimeSamplerConfiguration configuration,
            uint authority,
            ref RuntimeSamplerState state);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", EntryPoint = "ApplyContinuousProfilerConfiguration", CallingConvention = CallingConvention.Winapi)]
        private static extern int ApplyNonWindows(
            ref RuntimeSamplerConfiguration configuration,
            uint authority,
            ref RuntimeSamplerState state);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", EntryPoint = "GetContinuousProfilerState", CallingConvention = CallingConvention.Winapi)]
        private static extern int GetStateWindows(ref RuntimeSamplerState state);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", EntryPoint = "GetContinuousProfilerState", CallingConvention = CallingConvention.Winapi)]
        private static extern int GetStateNonWindows(ref RuntimeSamplerState state);
    }
}
