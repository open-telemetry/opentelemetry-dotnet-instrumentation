// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.GenericTests;

public class ComprehensiveCaller<TCaller1, TCaller2>
{
    #region CallReturnM1
    public void CallReturnM1WithCallerTypeArgs(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM1<TCaller1, TCaller2>(input1, input2);
    }

    public void CallReturnM1WithCallerTypeArgsReversed(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM1<TCaller2, TCaller1>(input2, input1);
    }

    public void CallReturnM1WithClass(GenericTarget<TCaller1, TCaller2> target, Exception input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif
        target.ReturnM1<Exception, TCaller2>(input1, input2);
    }

    public void CallReturnM1WithStruct(GenericTarget<TCaller1, TCaller2> target, PointStruct input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM1<PointStruct, TCaller2>(input1, input2);
    }

    public void CallReturnM1WithReferenceTypeGenericInstantiation(GenericTarget<TCaller1, TCaller2> target, Task<Exception> input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM1<Task<Exception>, TCaller2>(input1, input2);
    }

    public void CallReturnM1WithValueTypeGenericInstantiation(GenericTarget<TCaller1, TCaller2> target, StructContainer<Exception> input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM1<StructContainer<Exception>, TCaller2>(input1, input2);
    }
    #endregion

    #region CallReturnM2
    public void CallReturnM2WithCallerTypeArgs(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM2<TCaller1, TCaller2>(input1, input2);
    }

    public void CallReturnM2WithCallerTypeArgsReversed(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, TCaller2 input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM2<TCaller2, TCaller1>(input2, input1);
    }

    public void CallReturnM2WithClass(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, Exception input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM2<TCaller1, Exception>(input1, input2);
    }

    public void CallReturnM2WithStruct(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, PointStruct input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM2<TCaller1, PointStruct>(input1, input2);
    }

    public void CallReturnM2WithReferenceTypeGenericInstantiation(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, Task<Exception> input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM2<TCaller1, Task<Exception>>(input1, input2);
    }

    public void CallReturnM2WithValueTypeGenericInstantiation(GenericTarget<TCaller1, TCaller2> target, TCaller1 input1, StructContainer<Exception> input2)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnM2<TCaller1, StructContainer<Exception>>(input1, input2);
    }
    #endregion

    #region CallReturnT1
    public void CallReturnT1WithCallerTypeArgs(GenericTarget<TCaller1, TCaller2> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT1(input);
    }

    public void CallReturnT1WithCallerTypeArgsReversed(GenericTarget<TCaller2, TCaller1> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT1(input);
    }

    public void CallReturnT1WithClass(GenericTarget<Exception, TCaller2> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT1(input);
    }

    public void CallReturnT1WithStruct(GenericTarget<PointStruct, TCaller2> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT1(input);
    }

    public void CallReturnT1WithReferenceTypeGenericInstantiation(GenericTarget<Task<Exception>, TCaller2> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT1(input);
    }

    public void CallReturnT1WithValueTypeGenericInstantiation(GenericTarget<StructContainer<Exception>, TCaller2> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT1(input);
    }
    #endregion

    #region CallReturnT2
    public void CallReturnT2WithCallerTypeArgs(GenericTarget<TCaller1, TCaller2> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT2(input);
    }

    public void CallReturnT2WithCallerTypeArgsReversed(GenericTarget<object, TCaller1> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT2(input);
    }

    public void CallReturnT2WithClass(GenericTarget<int, Exception> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT2(input);
    }

    public void CallReturnT2WithStruct(GenericTarget<StructContainer<Exception>, PointStruct> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT2(input);
    }

    public void CallReturnT2WithReferenceTypeGenericInstantiation(GenericTarget<Task<Task<Task<Exception>>>, Task<Exception>> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT2(input);
    }

    public void CallReturnT2WithValueTypeGenericInstantiation(GenericTarget<PointStruct, StructContainer<Exception>> target, object input)
    {
#if NET
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        target.ReturnT2(input);
    }
    #endregion
}
