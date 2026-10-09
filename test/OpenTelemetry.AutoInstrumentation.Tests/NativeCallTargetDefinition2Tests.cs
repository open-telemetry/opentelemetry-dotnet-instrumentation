// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public sealed class NativeCallTargetDefinition2Tests
{
    [Fact]
    public void V2PreservesV1Prefix()
    {
        var v1Size = Marshal.SizeOf<NativeCallTargetDefinition>();

        Assert.Equal(v1Size, Marshal.OffsetOf<NativeCallTargetDefinition2>(nameof(NativeCallTargetDefinition2.Kind)).ToInt32());
        Assert.Equal(v1Size + 4, Marshal.OffsetOf<NativeCallTargetDefinition2>(nameof(NativeCallTargetDefinition2.Categories)).ToInt32());
        Assert.Equal(v1Size + 8, Marshal.SizeOf<NativeCallTargetDefinition2>());
    }
}
