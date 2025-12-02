// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

internal static class NativeMethods
{
    private static readonly bool IsWindows = string.Equals(FrameworkDescription.Instance.OSPlatform, "Windows", StringComparison.OrdinalIgnoreCase);

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

#if NET
    public static void ConfigureNativeContinuousProfiler(bool threadSamplingEnabled, uint threadSamplingInterval, bool allocationSamplingEnabled, uint maxMemorySamplesPerMinute, uint selectedThreadSamplingInterval)
    {
        if (IsWindows)
        {
            Windows.ConfigureContinuousProfiler(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, selectedThreadSamplingInterval);
        }
        else
        {
            NonWindows.ConfigureContinuousProfiler(threadSamplingEnabled, threadSamplingInterval, allocationSamplingEnabled, maxMemorySamplesPerMinute, selectedThreadSamplingInterval);
        }
    }

    public static int ContinuousProfilerReadThreadSamples(int len, byte[] buf)
    {
        return IsWindows ? Windows.ContinuousProfilerReadThreadSamples(len, buf) : NonWindows.ContinuousProfilerReadThreadSamples(len, buf);
    }

    public static int ContinuousProfilerReadAllocationSamples(int len, byte[] buf)
    {
        return IsWindows ? Windows.ContinuousProfilerReadAllocationSamples(len, buf) : NonWindows.ContinuousProfilerReadAllocationSamples(len, buf);
    }

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

        return ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow);
    }

    private static bool TryParseSpanContext(Activity currentActivity, out ulong traceIdHigh, out ulong traceIdLow, out ulong spanId)
    {
        traceIdLow = 0;
        traceIdHigh = 0;
        spanId = 0;
        var hexTraceId = currentActivity.TraceId.ToHexString();

        return ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow) &&
               ulong.TryParse(currentActivity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out spanId);
    }
#endif

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

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void ConfigureContinuousProfiler(bool threadSamplingEnabled, uint threadSamplingInterval, bool allocationSamplingEnabled, uint maxMemorySamplesPerMinute, uint selectedThreadSamplingInterval);

#if NET

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern int ContinuousProfilerReadThreadSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern int ContinuousProfilerReadAllocationSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern int SelectiveSamplerReadThreadSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void ContinuousProfilerNotifySpanStopped(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void SelectiveSamplingStart(ulong traceIdHigh, ulong traceIdLow);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void SelectiveSamplingStop(ulong traceIdHigh, ulong traceIdLow);
#endif

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

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void ConfigureContinuousProfiler(bool threadSamplingEnabled, uint threadSamplingInterval, bool allocationSamplingEnabled, uint maxMemorySamplesPerMinute, uint selectedThreadSamplingInterval);

#if NET
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern int ContinuousProfilerReadThreadSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern int ContinuousProfilerReadAllocationSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern int SelectiveSamplerReadThreadSamples(int len, byte[] buf);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void ContinuousProfilerNotifySpanStopped(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void SelectiveSamplingStart(ulong traceIdHigh, ulong traceIdLow);

        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void SelectiveSamplingStop(ulong traceIdHigh, ulong traceIdLow);
#endif
    }
}
