// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Reflection;
#endif
using System.Runtime.InteropServices;

namespace TestApplication.ContinuousProfiler;

internal static class RuntimeContinuousProfilerNativeMethods
{
#if NET
    private const string NativeLibraryName = "OpenTelemetry.AutoInstrumentation.Native";

    static RuntimeContinuousProfilerNativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(RuntimeContinuousProfilerNativeMethods).Assembly, ImportResolver);
    }
#endif

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ConfigureContinuousProfiler(
        bool threadSamplingEnabled,
        uint threadSamplingInterval,
        bool threadSamplingExportPipelinePrepared,
        bool allocationSamplingEnabled,
        uint maxMemorySamplesPerMinute,
        bool allocationSamplingExportPipelinePrepared,
        uint selectedThreadSamplingInterval);

#if NET
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [DllImport(NativeLibraryName)]
    public static extern uint GetContinuousProfilerAllocationSamplingRate();
#endif

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetContinuousProfilerSamplingInterval(uint threadSamplingInterval);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetContinuousProfilerEnabled(bool enabled);

#if NET
    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == NativeLibraryName)
        {
            var profilerPath = Environment.GetEnvironmentVariable("CORECLR_PROFILER_PATH");
            if (!string.IsNullOrWhiteSpace(profilerPath))
            {
                return NativeLibrary.Load(profilerPath);
            }
        }

        return IntPtr.Zero;
    }
#endif
}
