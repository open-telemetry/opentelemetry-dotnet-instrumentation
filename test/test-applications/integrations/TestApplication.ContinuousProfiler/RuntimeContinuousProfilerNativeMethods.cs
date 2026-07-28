// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace TestApplication.ContinuousProfiler;

internal static class RuntimeContinuousProfilerNativeMethods
{
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
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
    [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    public static extern void SetContinuousProfilerSamplingInterval(uint threadSamplingInterval);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
#if NET
    [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
#else
    [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
#endif
    public static extern void SetContinuousProfilerEnabled(bool enabled);
}
