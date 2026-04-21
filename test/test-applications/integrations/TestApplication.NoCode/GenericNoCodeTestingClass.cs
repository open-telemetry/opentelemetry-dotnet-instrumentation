// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.NoCode;

internal sealed class GenericNoCodeTestingClass<TFooClass, TBarClass>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CA1822 // Mark members as static
    public TFooMethod GenericTestMethod<TFooMethod, TBarMethod>(TFooMethod fooMethod, TBarMethod barMethod, TFooClass fooClass, TBarClass barClass)
#pragma warning restore CA1822 // Mark members as static
    {
        return fooMethod;
    }
}
