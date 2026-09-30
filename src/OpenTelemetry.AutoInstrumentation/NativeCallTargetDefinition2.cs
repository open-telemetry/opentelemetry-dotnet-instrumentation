// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

// Keep this layout in sync with CallTargetDefinition2 in the native profiler.
// The unused V1 layout remains available to simplify future upstream synchronization.
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeCallTargetDefinition2
{
    [MarshalAs(UnmanagedType.LPWStr)]
    public readonly string TargetAssembly;

    [MarshalAs(UnmanagedType.LPWStr)]
    public readonly string TargetType;

    [MarshalAs(UnmanagedType.LPWStr)]
    public readonly string TargetMethod;

    public readonly IntPtr TargetSignatureTypes;

    public readonly ushort TargetSignatureTypesLength;

    public readonly ushort TargetMinimumMajor;

    public readonly ushort TargetMinimumMinor;

    public readonly ushort TargetMinimumPatch;

    public readonly ushort TargetMaximumMajor;

    public readonly ushort TargetMaximumMinor;

    public readonly ushort TargetMaximumPatch;

    [MarshalAs(UnmanagedType.LPWStr)]
    public readonly string IntegrationAssembly;

    [MarshalAs(UnmanagedType.LPWStr)]
    public readonly string IntegrationType;

    public readonly byte Kind;

    public readonly uint Categories;

    public NativeCallTargetDefinition2(
        string targetAssembly,
        string targetType,
        string targetMethod,
        string[] targetSignatureTypes,
        ushort targetMinimumMajor,
        ushort targetMinimumMinor,
        ushort targetMinimumPatch,
        ushort targetMaximumMajor,
        ushort targetMaximumMinor,
        ushort targetMaximumPatch,
        string integrationAssembly,
        string integrationType,
        byte kind,
        uint categories)
    {
        TargetAssembly = targetAssembly;
        TargetType = targetType;
        TargetMethod = targetMethod;
        TargetSignatureTypes = IntPtr.Zero;
        if (targetSignatureTypes?.Length > 0)
        {
            TargetSignatureTypes = Marshal.AllocHGlobal(targetSignatureTypes.Length * Marshal.SizeOf<IntPtr>());
            var ptr = TargetSignatureTypes;
            for (var i = 0; i < targetSignatureTypes.Length; i++)
            {
                Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalUni(targetSignatureTypes[i]));
                ptr += Marshal.SizeOf<IntPtr>();
            }
        }

        TargetSignatureTypesLength = (ushort)(targetSignatureTypes?.Length ?? 0);
        TargetMinimumMajor = targetMinimumMajor;
        TargetMinimumMinor = targetMinimumMinor;
        TargetMinimumPatch = targetMinimumPatch;
        TargetMaximumMajor = targetMaximumMajor;
        TargetMaximumMinor = targetMaximumMinor;
        TargetMaximumPatch = targetMaximumPatch;
        IntegrationAssembly = integrationAssembly;
        IntegrationType = integrationType;
        Kind = kind;
        Categories = categories;
    }

    public void Dispose()
    {
        var ptr = TargetSignatureTypes;
        for (var i = 0; i < TargetSignatureTypesLength; i++)
        {
            Marshal.FreeHGlobal(Marshal.ReadIntPtr(ptr));
            ptr += Marshal.SizeOf<IntPtr>();
        }

        Marshal.FreeHGlobal(TargetSignatureTypes);
    }
}
