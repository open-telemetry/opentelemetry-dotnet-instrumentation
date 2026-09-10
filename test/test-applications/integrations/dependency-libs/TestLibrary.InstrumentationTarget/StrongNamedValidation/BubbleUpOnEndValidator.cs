// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

public static class BubbleUpOnEndValidator
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    {
        throw new CallTargetBubbleUpException("Bubble-up exception from OnMethodEnd.");
    }
}
