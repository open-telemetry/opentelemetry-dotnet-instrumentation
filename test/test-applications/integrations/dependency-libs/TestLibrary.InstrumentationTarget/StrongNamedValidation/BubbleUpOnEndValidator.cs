// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

public static class BubbleUpOnEndValidator
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    {
        var bubbleUpException = new CallTargetBubbleUpException("Bubble-up exception from OnMethodEnd.");
        if (instance is Command { WrapBubbleUpExceptions: true })
        {
            throw new InvalidOperationException("Wrapped OnMethodEnd failure.", new TargetInvocationException(bubbleUpException));
        }

        throw bubbleUpException;
    }
}
