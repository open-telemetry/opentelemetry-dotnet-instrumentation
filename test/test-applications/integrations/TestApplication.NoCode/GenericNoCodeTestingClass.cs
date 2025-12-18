// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.NoCode;

internal class GenericNoCodeTestingClass<TFooClass, TBarClass>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public TFooMethod GenericTestMethod<TFooMethod, TBarMethod>(TFooMethod fooMethod, TBarMethod barMethod, TFooClass fooClass, TBarClass barClass)
    {
        return fooMethod;
    }
}
