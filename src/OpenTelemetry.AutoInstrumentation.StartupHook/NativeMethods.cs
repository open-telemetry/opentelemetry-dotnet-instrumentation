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
