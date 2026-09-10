// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

public static class BubbleUpOnBeginValidator
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        throw new CallTargetBubbleUpException("Bubble-up exception from OnMethodBegin.");
    }
}
