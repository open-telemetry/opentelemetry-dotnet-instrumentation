// <copyright file="NativeMethods.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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

    [DllImport("OpenTelemetry.AutoInstrumentation.Native", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsProfilerAttached();

    private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        IntPtr libHandle = IntPtr.Zero;

        if (libraryName == "OpenTelemetry.AutoInstrumentation.Native")
        {
            libHandle = NativeLibrary.Load(GetProfilerPath());
        }

        return libHandle;
    }

    private static string GetProfilerPath()
    {
        // Try get CORECLR_PROFILER_PATH_64
        if (Environment.Is64BitProcess && TryRetrieveProfilerPath(Profiler64BitPathVariable, out var profiler64Path))
        {
            return profiler64Path!;
        }

        // Try get CORECLR_PROFILER_PATH_32
        if (!Environment.Is64BitProcess && TryRetrieveProfilerPath(Profiler32BitPathVariable, out var profiler32Path))
        {
            return profiler32Path!;
        }

        // Try get CORECLR_PROFILER_PATH
        if (TryRetrieveProfilerPath(ProfilerPathVariable, out var profilerPath))
        {
            return profilerPath!;
        }

        throw new DllNotFoundException($"Could not find native profiler path");
    }

    private static bool TryRetrieveProfilerPath(string pathVariable, out string? profilerPath)
    {
        var profilerPathValue = EnvironmentHelper.GetEnvironmentVariable(pathVariable);
        if (!string.IsNullOrWhiteSpace(profilerPathValue))
        {
            profilerPath = profilerPathValue;
            return true;
        }

        profilerPath = null;
        return false;
    }
}
