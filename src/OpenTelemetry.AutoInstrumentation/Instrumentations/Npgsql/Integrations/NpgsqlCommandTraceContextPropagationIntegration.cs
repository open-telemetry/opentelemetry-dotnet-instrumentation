// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql.Integrations;

/// <summary>
/// Npgsql command trace context propagation instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: NpgsqlConstants.AssemblyName,
    typeName: NpgsqlConstants.CommandTypeName,
    methodName: NpgsqlConstants.TraceCommandStartMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [NpgsqlConstants.ConnectorTypeName],
    minimumVersion: NpgsqlConstants.MinVersion,
    maximumVersion: NpgsqlConstants.TraceCommandStartMaxVersion,
    integrationName: NpgsqlConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: NpgsqlConstants.AssemblyName,
    typeName: NpgsqlConstants.CommandTypeName,
    methodName: NpgsqlConstants.TraceCommandEnrichMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [NpgsqlConstants.ConnectorTypeName],
    minimumVersion: NpgsqlConstants.TraceCommandEnrichMinVersion,
    maximumVersion: NpgsqlConstants.MaxVersion,
    integrationName: NpgsqlConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class NpgsqlCommandTraceContextPropagationIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TConnector>(TTarget instance, TConnector connector)
    {
        return new CallTargetState(null, connector);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is null)
        {
            NpgsqlTraceContextPropagator.Propagate(instance, state.State);
        }

        return CallTargetReturn.GetDefault();
    }
}
