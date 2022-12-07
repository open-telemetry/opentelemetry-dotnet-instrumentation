// <copyright file="StrongNamedValidation.cs" company="OpenTelemetry Authors">
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

using System;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Validations;

/// <summary>
/// Instrumentation targeting the test application used to validate the strong name scenario.
/// When an actual bytecode instrumentation targeting a strong named assembly on .NET Framework
/// is added we can remove this instrumentation.
/// </summary>
[InstrumentMethod(
    AssemblyName = "TestLibrary.InstrumentationTarget",
    TypeName = "TestLibrary.InstrumentationTarget.Command",
    MethodName = "Execute",
    ReturnTypeName = ClrNames.Void,
    ParameterTypeNames = new string[0],
    MinimumVersion = "1.0.0",
    MaximumVersion = "1.65535.65535",
    IntegrationName = "StrongNamedValidation",
    Type = InstrumentationType.Trace)]
public static class StrongNamedValidation
{
    private static readonly ActivitySource ValidationActivitySource = new ActivitySource("TestApplication.StrongNamedValidation");

    /// <summary>
    /// OnMethodBegin callback.
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        using var activity = ValidationActivitySource.StartActivity(nameof(StrongNamedValidation));
        Console.WriteLine($"Validation: {nameof(StrongNamedValidation)}");
        return CallTargetState.GetDefault();
    }
}
