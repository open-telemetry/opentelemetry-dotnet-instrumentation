// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

public static class RegularExceptionValidator
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        throw new InvalidOperationException("This integration exception must be swallowed by CallTarget.");
    }
}
