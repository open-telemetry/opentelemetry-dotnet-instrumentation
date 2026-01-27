// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

/// <summary>
/// Instrumentation targeting the test application used to validate the strong name scenario.
/// When an actual bytecode instrumentation targeting a strong named assembly on .NET Framework
/// is added we can remove this instrumentation.
/// </summary>
// [InstrumentMethod(
//    assemblyName: "TestLibrary.InstrumentationTarget",
//    typeName: "TestLibrary.InstrumentationTarget.Command",
//    methodName: "Execute",
//    returnTypeName: ClrNames.Void,
//    parameterTypeNames: new string[0],
//    minimumVersion: "1.0.0",
//    maximumVersion: "1.65535.65535",
//    integrationName: "StrongNamedValidation",
//    type: InstrumentationType.Trace)]
public static class Validator
{
    private static readonly ActivitySource ValidationActivitySource = new("ByteCode.Plugin.StrongNamedValidation");

    /// <summary>
    /// OnMethodBegin callback.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        using var activity = ValidationActivitySource.StartActivity(nameof(Validator));
        Console.WriteLine($"Validator: {typeof(Validator).FullName}");
        return CallTargetState.GetDefault();
    }
}
