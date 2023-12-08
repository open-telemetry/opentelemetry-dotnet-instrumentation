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

    // the "dll" extension is required on .NET Framework
    // and optional on .NET Core
    private static class Windows
    {
        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native.dll")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);
    }

    // assume .NET Core if not running on Windows
    private static class NonWindows
    {
        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);

        [DllImport("OpenTelemetry.AutoInstrumentation.Native")]
        public static extern void AddDerivedInstrumentations([MarshalAs(UnmanagedType.LPWStr)] string id, [In] NativeCallTargetDefinition[] methodArrays, int size);
    }
}
