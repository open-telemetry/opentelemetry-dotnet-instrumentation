// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.GenericTests;

public class GenericTarget<T1, T2>
{
    public TMethod1 ReturnM1<TMethod1, TMethod2>(TMethod1 input1, TMethod2 input2)
    {
        return input1;
    }

    public TMethod2 ReturnM2<TMethod1, TMethod2>(TMethod1 input1, TMethod2 input2)
    {
        return input2;
    }

    public T1 ReturnT1(object input)
    {
        return (T1)input;
    }

    public T2 ReturnT2(object input)
    {
        return (T2)input;
    }
}
