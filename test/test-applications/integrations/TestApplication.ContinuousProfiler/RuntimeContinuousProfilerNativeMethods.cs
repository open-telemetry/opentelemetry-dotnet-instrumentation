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
    public static extern void ConfigureContinuousProfiler(
        bool threadSamplingEnabled,
        uint threadSamplingInterval,
        bool allocationSamplingEnabled,
        uint maxMemorySamplesPerMinute,
        uint selectedThreadSamplingInterval);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    public static extern void SetContinuousProfilerSamplingInterval(uint threadSamplingInterval);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    public static extern void SetContinuousProfilerEnabled(bool enabled);

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
