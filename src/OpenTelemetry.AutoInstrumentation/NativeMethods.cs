// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

internal static class NativeMethods
{
    private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static void AddInstrumentations(string id, NativeCallTargetDefinition[] methodArrays)
    {
        if (methodArrays is null || methodArrays.Length == 0)
        {
            return;
        }

        if (IsWindows)
        {
            Windows.AddInstrumentations(id, methodArrays, methodArrays.Length);
        }
        else
        {
            NonWindows.AddInstrumentations(id, methodArrays, methodArrays.Length);
        }
    }

    public static void AddDerivedInstrumentations(string id, NativeCallTargetDefinition[] methodArrays)
    {
        if (methodArrays is null || methodArrays.Length == 0)
        {
            return;
        }

        if (IsWindows)
        {
            Windows.AddDerivedInstrumentations(id, methodArrays, methodArrays.Length);
        }
        else
        {
            NonWindows.AddDerivedInstrumentations(id, methodArrays, methodArrays.Length);
        }
    }

#if NETFRAMEWORK
    public static void SetSqlClientNetFxILRewriteEnabled(bool enabled)
    {
        if (IsWindows)
        {
            Windows.SetSqlClientNetFxILRewriteEnabled(enabled);
        }
        else
        {
            NonWindows.SetSqlClientNetFxILRewriteEnabled(enabled);
        }
    }
#endif

    public static bool ConfigureNativeContinuousProfiler(
        bool threadSamplingEnabled,
        uint threadSamplingInterval,
        bool threadSamplingExportPipelinePrepared,
        bool allocationSamplingEnabled,
        uint maxMemorySamplesPerMinute,
        bool allocationSamplingExportPipelinePrepared,
        uint selectedThreadSamplingInterval,
        out bool isInitializationOwner)
    {
        if (IsWindows)
        {
            return Windows.ConfigureContinuousProfiler(
                threadSamplingEnabled,
                threadSamplingInterval,
                threadSamplingExportPipelinePrepared,
                allocationSamplingEnabled,
                maxMemorySamplesPerMinute,
                allocationSamplingExportPipelinePrepared,
                selectedThreadSamplingInterval,
                out isInitializationOwner);
        }
        else
        {
            return NonWindows.ConfigureContinuousProfiler(
                threadSamplingEnabled,
                threadSamplingInterval,
                threadSamplingExportPipelinePrepared,
                allocationSamplingEnabled,
                maxMemorySamplesPerMinute,
                allocationSamplingExportPipelinePrepared,
                selectedThreadSamplingInterval,
                out isInitializationOwner);
        }
    }

    public static bool ShutdownNativeContinuousProfiler()
    {
        return IsWindows
            ? Windows.ShutdownContinuousProfiler()
            : NonWindows.ShutdownContinuousProfiler();
    }

    public static bool SetNativeContinuousProfilerSamplingInterval(uint threadSamplingInterval)
    {
        if (IsWindows)
        {
            return Windows.SetContinuousProfilerSamplingInterval(threadSamplingInterval);
        }
        else
        {
            return NonWindows.SetContinuousProfilerSamplingInterval(threadSamplingInterval);
        }
    }

    public static bool SetNativeContinuousProfilerEnabled(bool enabled)
    {
        if (IsWindows)
        {
            return Windows.SetContinuousProfilerEnabled(enabled);
        }
        else
        {
            return NonWindows.SetContinuousProfilerEnabled(enabled);
        }
    }

    public static bool SetNativeContinuousProfilerAllocationSamplingEnabled(bool enabled)
    {
        return IsWindows
            ? Windows.SetContinuousProfilerAllocationSamplingEnabled(enabled)
            : NonWindows.SetContinuousProfilerAllocationSamplingEnabled(enabled);
    }

    public static bool SetNativeContinuousProfilerMaxMemorySamplesPerMinute(uint maxMemorySamplesPerMinute)
    {
        return IsWindows
            ? Windows.SetContinuousProfilerMaxMemorySamplesPerMinute(maxMemorySamplesPerMinute)
            : NonWindows.SetContinuousProfilerMaxMemorySamplesPerMinute(maxMemorySamplesPerMinute);
    }

    public static bool SetNativeContinuousProfilerSnapshotsEnabled(bool enabled)
    {
        return IsWindows
            ? Windows.SetContinuousProfilerSnapshotsEnabled(enabled)
            : NonWindows.SetContinuousProfilerSnapshotsEnabled(enabled);
    }

    public static bool SetNativeContinuousProfilerSnapshotSamplingInterval(uint snapshotSamplingInterval)
    {
        return IsWindows
            ? Windows.SetContinuousProfilerSnapshotSamplingInterval(snapshotSamplingInterval)
            : NonWindows.SetContinuousProfilerSnapshotSamplingInterval(snapshotSamplingInterval);
    }

    public static uint GetNativeContinuousProfilerSamplingInterval()
    {
        return IsWindows
            ? Windows.GetContinuousProfilerSamplingInterval()
            : NonWindows.GetContinuousProfilerSamplingInterval();
    }

    public static int ContinuousProfilerReadThreadSamples(int len, byte[] buf, out uint samplingInterval)
    {
        return IsWindows
            ? Windows.ContinuousProfilerReadThreadSamples(len, buf, out samplingInterval)
            : NonWindows.ContinuousProfilerReadThreadSamples(len, buf, out samplingInterval);
    }

#if NET
    public static int ContinuousProfilerReadAllocationSamples(int len, byte[] buf)
    {
        return IsWindows ? Windows.ContinuousProfilerReadAllocationSamples(len, buf) : NonWindows.ContinuousProfilerReadAllocationSamples(len, buf);
    }
#endif

    public static int SelectiveSamplerReadThreadSamples(int len, byte[] buf)
    {
        return IsWindows ? Windows.SelectiveSamplerReadThreadSamples(len, buf) : NonWindows.SelectiveSamplerReadThreadSamples(len, buf);
    }

    public static void ContinuousProfilerResetNativeContext()
    {
        if (IsWindows)
        {
            Windows.ContinuousProfilerSetNativeContext(0, 0, 0);
        }
        else
        {
            NonWindows.ContinuousProfilerSetNativeContext(0, 0, 0);
        }
    }

    public static void ContinuousProfilerSetNativeContext(Activity activity)
    {
        if (!TryParseSpanContext(activity, out var traceIdHigh, out var traceIdLow, out var spanId))
        {
            return;
        }

        if (IsWindows)
        {
            Windows.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId);
        }
        else
        {
            NonWindows.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId);
        }
    }

    public static void ContinuousProfilerNotifySpanStopped(Activity activity)
    {
        if (!TryParseSpanContext(activity, out var traceIdHigh, out var traceIdLow, out var spanId))
        {
            return;
        }

        if (IsWindows)
        {
            Windows.ContinuousProfilerNotifySpanStopped(traceIdHigh, traceIdLow, spanId);
        }
        else
        {
            NonWindows.ContinuousProfilerNotifySpanStopped(traceIdHigh, traceIdLow, spanId);
        }
    }

    public static void SelectiveSamplingStart(ActivityTraceId traceId)
    {
        if (!TryParseTraceContext(traceId, out var traceIdHigh, out var traceIdLow))
        {
            return;
        }

        if (IsWindows)
        {
            Windows.SelectiveSamplingStart(traceIdHigh, traceIdLow);
        }
        else
        {
            NonWindows.SelectiveSamplingStart(traceIdHigh, traceIdLow);
        }
    }

    public static void SelectiveSamplingStop(ActivityTraceId traceId)
    {
        if (!TryParseTraceContext(traceId, out var traceIdHigh, out var traceIdLow))
        {
            return;
        }

        if (IsWindows)
        {
            Windows.SelectiveSamplingStop(traceIdHigh, traceIdLow);
        }
        else
        {
            NonWindows.SelectiveSamplingStop(traceIdHigh, traceIdLow);
        }
    }

    private static bool TryParseTraceContext(ActivityTraceId currentActivityTraceId, out ulong traceIdHigh, out ulong traceIdLow)
    {
        traceIdLow = 0;
        traceIdHigh = 0;
        var hexTraceId = currentActivityTraceId.ToHexString();
#if NET
        return ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow);
#else
        return ulong.TryParse(hexTraceId.Substring(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.Substring(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow);
#endif
    }

    private static bool TryParseSpanContext(Activity currentActivity, out ulong traceIdHigh, out ulong traceIdLow, out ulong spanId)
    {
        traceIdLow = 0;
        traceIdHigh = 0;
        spanId = 0;
        var hexTraceId = currentActivity.TraceId.ToHexString();
#if NET
        return ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow) &&
               ulong.TryParse(currentActivity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out spanId);
#else
        return ulong.TryParse(hexTraceId.Substring(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.Substring(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow) &&
               ulong.TryParse(currentActivity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out spanId);

#endif
    }

    // the "dll" extension is required on .NET Framework
    // and optional on .NET Core
    private static class Windows
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

#if NETFRAMEWORK
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void SetSqlClientNetFxILRewriteEnabled(bool enabled);
#endif

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ConfigureContinuousProfiler(
            bool threadSamplingEnabled,
            uint threadSamplingInterval,
            bool threadSamplingExportPipelinePrepared,
            bool allocationSamplingEnabled,
            uint maxMemorySamplesPerMinute,
            bool allocationSamplingExportPipelinePrepared,
            uint selectedThreadSamplingInterval,
            [MarshalAs(UnmanagedType.Bool)] out bool isInitializationOwner);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShutdownContinuousProfiler();

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerSamplingInterval(uint threadSamplingInterval);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerEnabled(bool enabled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerAllocationSamplingEnabled(bool enabled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerMaxMemorySamplesPerMinute(uint maxMemorySamplesPerMinute);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerSnapshotsEnabled(bool enabled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerSnapshotSamplingInterval(uint snapshotSamplingInterval);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ContinuousProfilerReadThreadSamples(int len, byte[] buf, out uint samplingInterval);

#if NET
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ContinuousProfilerReadAllocationSamples(int len, byte[] buf);
#endif

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SelectiveSamplerReadThreadSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ContinuousProfilerNotifySpanStopped(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SelectiveSamplingStart(ulong traceIdHigh, ulong traceIdLow);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SelectiveSamplingStop(ulong traceIdHigh, ulong traceIdLow);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern uint GetContinuousProfilerSamplingInterval();
    }

    // assume .NET Core if not running on Windows
    private static class NonWindows
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

#if NETFRAMEWORK
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void SetSqlClientNetFxILRewriteEnabled(bool enabled);
#endif

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ConfigureContinuousProfiler(
            bool threadSamplingEnabled,
            uint threadSamplingInterval,
            bool threadSamplingExportPipelinePrepared,
            bool allocationSamplingEnabled,
            uint maxMemorySamplesPerMinute,
            bool allocationSamplingExportPipelinePrepared,
            uint selectedThreadSamplingInterval,
            [MarshalAs(UnmanagedType.Bool)] out bool isInitializationOwner);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShutdownContinuousProfiler();

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerSamplingInterval(uint threadSamplingInterval);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerEnabled(bool enabled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerAllocationSamplingEnabled(bool enabled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerMaxMemorySamplesPerMinute(uint maxMemorySamplesPerMinute);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerSnapshotsEnabled(bool enabled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetContinuousProfilerSnapshotSamplingInterval(uint snapshotSamplingInterval);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ContinuousProfilerReadThreadSamples(int len, byte[] buf, out uint samplingInterval);

#if NET
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ContinuousProfilerReadAllocationSamples(int len, byte[] buf);
#endif

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern int SelectiveSamplerReadThreadSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ContinuousProfilerNotifySpanStopped(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SelectiveSamplingStart(ulong traceIdHigh, ulong traceIdLow);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SelectiveSamplingStop(ulong traceIdHigh, ulong traceIdLow);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern uint GetContinuousProfilerSamplingInterval();
    }
}
