// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace TestApplication.ContinuousProfiler;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct RuntimeSamplerState
{
    public uint StructureSize;
    public uint Authority;
    public RuntimeSamplerConfiguration CommittedConfiguration;
}
