// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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

    public static void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId)
    {
        if (IsWindows)
        {
            Windows.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId);
        }
        else
        {
            NonWindows.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId);
        }
    }

    public static void SelectiveSamplingStart()
    {
        if (IsWindows)
        {
            Windows.SelectiveSamplingStart();
        }
        else
        {
            NonWindows.SelectiveSamplingStart();
        }
    }

    public static void SelectiveSamplingStop()
    {
        if (IsWindows)
        {
            Windows.SelectiveSamplingStop();
        }
        else
        {
            NonWindows.SelectiveSamplingStop();
        }
    }
#endif

    // the "dll" extension is required on .NET Framework
    // and optional on .NET Core
    private static class Windows
    {
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void ConfigureContinuousProfiler(bool threadSamplingEnabled, uint threadSamplingInterval, bool allocationSamplingEnabled, uint maxMemorySamplesPerMinute, uint selectedThreadSamplingInterval);

#if NET

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern int ContinuousProfilerReadThreadSamples(int len, byte[] buf);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern int ContinuousProfilerReadAllocationSamples(int len, byte[] buf);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern int SelectiveSamplerReadThreadSamples(int len, byte[] buf);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void SelectiveSamplingStart();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void SelectiveSamplingStop();
#endif

    }

    // assume .NET Core if not running on Windows
    private static class NonWindows
    {
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void ConfigureContinuousProfiler(bool threadSamplingEnabled, uint threadSamplingInterval, bool allocationSamplingEnabled, uint maxMemorySamplesPerMinute, uint selectedThreadSamplingInterval);

#if NET
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern int ContinuousProfilerReadThreadSamples(int len, byte[] buf);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern int ContinuousProfilerReadAllocationSamples(int len, byte[] buf);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern int SelectiveSamplerReadThreadSamples(int len, byte[] buf);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void ContinuousProfilerSetNativeContext(ulong traceIdHigh, ulong traceIdLow, ulong spanId);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void SelectiveSamplingStart();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void SelectiveSamplingStop();
#endif
    }
}
