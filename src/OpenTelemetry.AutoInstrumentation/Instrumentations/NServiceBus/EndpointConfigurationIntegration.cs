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
parameterTypeNames: [ClrNames.String],
minimumVersion: "8.0.0",
#if NETFRAMEWORK
maximumVersion: "8.65535.65535",
#else
maximumVersion: "9.65535.65535",
#endif
integrationName: "NServiceBus",
type: InstrumentationType.Trace)]
[InstrumentMethod(
assemblyName: "NServiceBus.Core",
typeName: "NServiceBus.EndpointConfiguration",
methodName: ".ctor",
returnTypeName: ClrNames.Void,
parameterTypeNames: [ClrNames.String],
minimumVersion: "8.0.0",
#if NETFRAMEWORK
maximumVersion: "8.65535.65535",
#else
maximumVersion: "9.65535.65535",
#endif
integrationName: "NServiceBus",
type: InstrumentationType.Metric)]
public static class EndpointConfigurationIntegration
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    {
        var openTelemetryConfigurationExtensionsType = Type.GetType("NServiceBus.OpenTelemetryConfigurationExtensions, NServiceBus.Core");
        var enableOpenTelemetryMethodInfo = openTelemetryConfigurationExtensionsType?.GetMethod("EnableOpenTelemetry");

        enableOpenTelemetryMethodInfo?.Invoke(null, [instance]);

        return CallTargetReturn.GetDefault();
    }
}
