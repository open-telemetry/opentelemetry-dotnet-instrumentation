// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

public static class BubbleUpOnBeginValidator
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        var exception = new CallTargetBubbleUpException("Bubble-up exception from OnMethodBegin.");
        if (instance is Command { WrapBubbleUpExceptions: true })
        {
            throw new InvalidOperationException("Wrapped OnMethodBegin failure.", new TargetInvocationException(exception));
        }

        throw exception;
    }
}
