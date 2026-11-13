// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

// Keep this layout in sync with CallTargetDefinition2 in the native profiler.
// The unused V1 layout remains available to simplify future upstream synchronization.
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeCallTargetDefinition2
{
    public readonly nint TargetAssembly;

    public readonly nint TargetType;

    public readonly nint TargetMethod;

    public readonly nint TargetSignatureTypes;

    public readonly ushort TargetSignatureTypesLength;

    public readonly ushort TargetMinimumMajor;

    public readonly ushort TargetMinimumMinor;

    public readonly ushort TargetMinimumPatch;

    public readonly ushort TargetMaximumMajor;

    public readonly ushort TargetMaximumMinor;

    public readonly ushort TargetMaximumPatch;

    public readonly nint IntegrationAssembly;

    public readonly nint IntegrationType;

    public readonly byte Kind;

    public readonly uint Categories;

    public NativeCallTargetDefinition2(
        nint targetAssembly,
        nint targetType,
        nint targetMethod,
        nint targetSignatureTypes,
        ushort targetSignatureTypesLength,
        ushort targetMinimumMajor,
        ushort targetMinimumMinor,
        ushort targetMinimumPatch,
        ushort targetMaximumMajor,
        ushort targetMaximumMinor,
        ushort targetMaximumPatch,
        nint integrationAssembly,
        nint integrationType,
        byte kind,
        uint categories)
    {
        TargetAssembly = targetAssembly;
        TargetType = targetType;
        TargetMethod = targetMethod;
        TargetSignatureTypes = targetSignatureTypes;
        TargetSignatureTypesLength = targetSignatureTypesLength;
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
        TargetAssembly = NativeCallTargetUnmanagedMemoryHelper.AllocateAndWriteUtf16String(targetAssembly);
        TargetType = NativeCallTargetUnmanagedMemoryHelper.AllocateAndWriteUtf16String(targetType);
        TargetMethod = NativeCallTargetUnmanagedMemoryHelper.AllocateAndWriteUtf16String(targetMethod);
        TargetSignatureTypes = NativeCallTargetUnmanagedMemoryHelper.AllocateAndWriteUtf16StringArray(targetSignatureTypes);
        TargetSignatureTypesLength = (ushort)(targetSignatureTypes?.Length ?? 0);
        TargetMinimumMajor = targetMinimumMajor;
        TargetMinimumMinor = targetMinimumMinor;
        TargetMinimumPatch = targetMinimumPatch;
        TargetMaximumMajor = targetMaximumMajor;
        TargetMaximumMinor = targetMaximumMinor;
        TargetMaximumPatch = targetMaximumPatch;
        IntegrationAssembly = NativeCallTargetUnmanagedMemoryHelper.AllocateAndWriteUtf16String(integrationAssembly);
        IntegrationType = NativeCallTargetUnmanagedMemoryHelper.AllocateAndWriteUtf16String(integrationType);
        Kind = kind;
        Categories = categories;
    }
}
