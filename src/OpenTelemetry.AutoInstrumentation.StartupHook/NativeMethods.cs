// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Helpers;

using static OpenTelemetry.AutoInstrumentation.Constants.EnvironmentVariables;

namespace OpenTelemetry.AutoInstrumentation;

internal static class NativeMethods
{
    static NativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ImportResolver);
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
    public static extern bool IsProfilerAttached();

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "OpenTelemetry.AutoInstrumentation.Native")
        {
            return NativeLibrary.Load(GetProfilerPath());
        }

        return IntPtr.Zero;
    }

    private static string GetProfilerPath()
    {
        // Bitness specific path has priority
        var bitnessSpecificProfilerPathVariable = Environment.Is64BitProcess ? Profiler64BitPathVariable : Profiler32BitPathVariable;
        if (TryRetrieveProfilerPath(bitnessSpecificProfilerPathVariable, out var bitnessSpecificProfilerPath))
        {
            return bitnessSpecificProfilerPath;
        }

        // Try get CORECLR_PROFILER_PATH
        if (TryRetrieveProfilerPath(ProfilerPathVariable, out var profilerPath))
        {
            return profilerPath;
        }

        throw new DllNotFoundException($"Could not find native profiler path");
    }

    private static bool TryRetrieveProfilerPath(string pathVariable, out string profilerPath)
    {
        var profilerPathValue = EnvironmentHelper.GetEnvironmentVariable(pathVariable);
        if (!string.IsNullOrWhiteSpace(profilerPathValue))
        {
            profilerPath = profilerPathValue;
            return true;
        }

        profilerPath = string.Empty;
        return false;
    }
}
