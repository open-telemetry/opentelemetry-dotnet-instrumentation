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
    public static extern bool ConfigureContinuousProfilerV2(
        bool threadSamplingEnabled,
        uint threadSamplingInterval,
        bool threadSamplingExportPipelinePrepared,
        bool allocationSamplingEnabled,
        uint maxMemorySamplesPerMinute,
        bool allocationSamplingExportPipelinePrepared,
        bool selectedThreadSamplingEnabled,
        bool selectedThreadSamplingExportPipelinePrepared,
        uint selectedThreadSamplingInterval,
        [MarshalAs(UnmanagedType.Bool)] out bool isInitializationOwner);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName, EntryPoint = "ConfigureContinuousProfiler")]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", EntryPoint = "ConfigureContinuousProfiler")]
#endif
    public static extern void ConfigureContinuousProfilerLegacy(
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
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShutdownContinuousProfiler();

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    public static extern uint GetContinuousProfilerSamplingInterval();

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

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetContinuousProfilerAllocationSamplingEnabled(bool enabled);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetContinuousProfilerMaxMemorySamplesPerMinute(uint maxMemorySamplesPerMinute);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetContinuousProfilerSnapshotsEnabled(bool enabled);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetContinuousProfilerSnapshotSamplingInterval(uint snapshotSamplingInterval);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(NativeLibraryName, CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
#endif
    public static extern int ContinuousProfilerReadThreadSamplesV2(
        int len,
        byte[] buffer,
        out uint samplingInterval);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport(
        NativeLibraryName,
        EntryPoint = "ContinuousProfilerReadThreadSamples",
        CallingConvention = CallingConvention.Cdecl)]
#else
    [DllImport(
        "OpenTelemetry.AutoInstrumentation.Native.dll",
        EntryPoint = "ContinuousProfilerReadThreadSamples",
        CallingConvention = CallingConvention.Cdecl)]
#endif
    public static extern int ContinuousProfilerReadThreadSamplesLegacy(int len, byte[] buffer);

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
