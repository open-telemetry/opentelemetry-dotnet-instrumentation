// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql.Integrations;

/// <summary>
/// Npgsql connector trace context cleanup instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: NpgsqlConstants.AssemblyName,
    typeName: NpgsqlConstants.ConnectorTypeName,
    methodName: NpgsqlConstants.EndUserActionMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [],
    minimumVersion: NpgsqlConstants.MinVersion,
    maximumVersion: NpgsqlConstants.MaxVersion,
    integrationName: NpgsqlConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class NpgsqlConnectorEndUserActionTraceContextPropagationIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        NpgsqlTraceContextPropagator.ClearOnUserActionEnd(instance);

        return CallTargetState.GetDefault();
    }
}
