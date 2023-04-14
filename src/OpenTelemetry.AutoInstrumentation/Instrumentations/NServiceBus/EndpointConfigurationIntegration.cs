// <copyright file="EndpointConfigurationIntegration.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NServiceBus;

/// <summary>
/// NServiceBus.EndpointConfigurationIntegration calltarget instrumentation
/// </summary>
[InstrumentMethod(
assemblyName: "NServiceBus.Core",
typeName: "NServiceBus.EndpointConfiguration",
methodName: ".ctor",
returnTypeName: ClrNames.Void,
parameterTypeNames: new[] { ClrNames.String },
minimumVersion: "8.0.0",
maximumVersion: "8.65535.65535",
integrationName: "NServiceBus",
type: InstrumentationType.Trace)]
[InstrumentMethod(
assemblyName: "NServiceBus.Core",
typeName: "NServiceBus.EndpointConfiguration",
methodName: ".ctor",
returnTypeName: ClrNames.Void,
parameterTypeNames: new[] { ClrNames.String },
minimumVersion: "8.0.0",
maximumVersion: "8.65535.65535",
integrationName: "NServiceBus",
type: InstrumentationType.Metric)]
public static class EndpointConfigurationIntegration
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    {
        var openTelemetryConfigurationExtensionsType = Type.GetType("NServiceBus.OpenTelemetryConfigurationExtensions, NServiceBus.Core");
        var enableOpenTelemetryMethodInfo = openTelemetryConfigurationExtensionsType?.GetMethod("EnableOpenTelemetry");

        enableOpenTelemetryMethodInfo?.Invoke(null, new object?[] { instance });

        return CallTargetReturn.GetDefault();
    }
}
