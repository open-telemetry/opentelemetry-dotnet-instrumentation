// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.GenericTests;

#pragma warning disable CA1815 // Override equals and operator equals on value types
public struct PointStruct
#pragma warning restore CA1815 // Override equals and operator equals on value types
{
#pragma warning disable CA1051 // Do not declare visible instance fields
    public int X;
    public int Y;
#pragma warning restore CA1051 // Do not declare visible instance fields

    public PointStruct(int x, int y)
    {
        X = x;
        Y = y;
    }
}
