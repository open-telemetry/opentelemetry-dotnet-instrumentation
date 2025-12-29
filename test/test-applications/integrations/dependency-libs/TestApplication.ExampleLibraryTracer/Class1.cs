// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibraryTracer;

public class Class1
{
#pragma warning disable CA1822 // Mark members as static
    public int Add(int x, int y)
#pragma warning restore CA1822 // Mark members as static
    {
        return 2 * (x + y);
    }

    public virtual int Multiply(int x, int y)
    {
        return 2 * (x * y);
    }
}
