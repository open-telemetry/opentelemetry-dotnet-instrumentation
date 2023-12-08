// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
