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

using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

internal static class NativeMethods
{
    private static readonly bool IsWindows = string.Equals(FrameworkDescription.Instance.OSPlatform, "Windows", StringComparison.OrdinalIgnoreCase);

    public static bool IsProfilerAttached()
    {
        if (IsWindows)
        {
            return Windows.IsProfilerAttached();
        }

        return NonWindows.IsProfilerAttached();
    }

    public static void InitializeProfiler(string id, NativeCallTargetDefinition[] methodArrays)
    {
        if (methodArrays is null || methodArrays.Length == 0)
        {
            return;
        }

        if (IsWindows)
        {
            Windows.InitializeProfiler(id, methodArrays, methodArrays.Length);
        }
        else
        {
            NonWindows.InitializeProfiler(id, methodArrays, methodArrays.Length);
        }
    }

    public static void EnableByRefInstrumentation()
    {
        if (IsWindows)
        {
            Windows.EnableByRefInstrumentation();
        }
        else
        {
            NonWindows.EnableByRefInstrumentation();
        }
    }

    public static void EnableCallTargetStateByRef()
    {
        if (IsWindows)
        {
            Windows.EnableCallTargetStateByRef();
        }
        else
        {
            NonWindows.EnableCallTargetStateByRef();
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

    // the "dll" extension is required on .NET Framework
    // and optional on .NET Core
    private static class Windows
    {
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern bool IsProfilerAttached();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void InitializeProfiler([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void EnableByRefInstrumentation();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void EnableCallTargetStateByRef();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);
    }

    // assume .NET Core if not running on Windows
    private static class NonWindows
    {
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern bool IsProfilerAttached();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void InitializeProfiler([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void EnableByRefInstrumentation();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void EnableCallTargetStateByRef();

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);
    }
}
