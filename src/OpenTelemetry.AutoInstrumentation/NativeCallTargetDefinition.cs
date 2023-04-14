// <copyright file="NativeCallTargetDefinition.cs" company="OpenTelemetry Authors">
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

using System;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation;

 // !                                         ██
 //                                         ██░░██
 //                                       ██░░░░░░██
 //                                     ██░░░░░░░░░░██
 //                                     ██░░░░░░░░░░██
 //                                   ██░░░░░░░░░░░░░░██
 //                                 ██░░░░░░██████░░░░░░██
 //                                 ██░░░░░░██████░░░░░░██
 //                               ██░░░░░░░░██████░░░░░░░░██
 //                               ██░░░░░░░░██████░░░░░░░░██
 //                             ██░░░░░░░░░░██████░░░░░░░░░░██
 //                           ██░░░░░░░░░░░░██████░░░░░░░░░░░░██
 //                           ██░░░░░░░░░░░░██████░░░░░░░░░░░░██
 //                         ██░░░░░░░░░░░░░░██████░░░░░░░░░░░░░░██
 //                         ██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██
 //                       ██░░░░░░░░░░░░░░░░██████░░░░░░░░░░░░░░░░██
 //                       ██░░░░░░░░░░░░░░░░██████░░░░░░░░░░░░░░░░██
 //                     ██░░░░░░░░░░░░░░░░░░██████░░░░░░░░░░░░░░░░░░██
 //                     ██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██
 //                       ██████████████████████████████████████████
 //
 // This struct is marshalled for use in the native layer, so this struct must be kept in sync with the _CallTargetDefinition native struct

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NativeCallTargetDefinition
{
    [MarshalAs(UnmanagedType.LPWStr)]
    public string InstrumentationName;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string InstrumentationType;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string TargetAssembly;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string TargetType;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string TargetMethod;

    public IntPtr TargetSignatureTypes;

    public ushort TargetSignatureTypesLength;

    public ushort TargetMinimumMajor;

    public ushort TargetMinimumMinor;

    public ushort TargetMinimumPatch;

    public ushort TargetMaximumMajor;

    public ushort TargetMaximumMinor;

    public ushort TargetMaximumPatch;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string IntegrationAssembly;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string IntegrationType;

    public NativeCallTargetDefinition(
            string instrumentationName,
            string instrumentationType,
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
            string integrationType)
    {
        InstrumentationName = instrumentationName;
        InstrumentationType = instrumentationType;
        TargetAssembly = targetAssembly;
        TargetType = targetType;
        TargetMethod = targetMethod;
        TargetSignatureTypes = IntPtr.Zero;
        if (targetSignatureTypes?.Length > 0)
        {
            TargetSignatureTypes = Marshal.AllocHGlobal(targetSignatureTypes.Length * Marshal.SizeOf(typeof(IntPtr)));
            var ptr = TargetSignatureTypes;
            for (var i = 0; i < targetSignatureTypes.Length; i++)
            {
                Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalUni(targetSignatureTypes[i]));
                ptr += Marshal.SizeOf(typeof(IntPtr));
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
    }

    public void Dispose()
    {
        var ptr = TargetSignatureTypes;
        for (var i = 0; i < TargetSignatureTypesLength; i++)
        {
            Marshal.FreeHGlobal(Marshal.ReadIntPtr(ptr));
            ptr += Marshal.SizeOf(typeof(IntPtr));
        }

        Marshal.FreeHGlobal(TargetSignatureTypes);
    }
}
