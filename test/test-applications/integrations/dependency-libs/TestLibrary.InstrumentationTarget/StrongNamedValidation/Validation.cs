// <copyright file="Validation.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
public static class Validation
{
    private static readonly ActivitySource ValidationActivitySource = new ActivitySource("ByteCode.Plugin.StrongNamedValidation");

    /// <summary>
    /// OnMethodBegin callback.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        using var activity = ValidationActivitySource.StartActivity(nameof(Validation));
        Console.WriteLine($"Validation: {typeof(Validation).FullName}");
        return CallTargetState.GetDefault();
    }
}
