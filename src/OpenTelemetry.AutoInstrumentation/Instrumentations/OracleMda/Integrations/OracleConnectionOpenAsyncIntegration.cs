// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda.Integrations;

/// <summary>
/// OracleConnection open async instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: OracleMdaConstants.OracleMdaAssemblyName,
    typeName: OracleMdaConstants.OracleConnectionTypeName,
    methodName: "OpenAsync",
    returnTypeName: ClrNames.Task,
    parameterTypeNames: [ClrNames.CancellationToken],
    minimumVersion: OracleMdaConstants.MinVersion,
    maximumVersion: OracleMdaConstants.MaxVersion,
    integrationName: OracleMdaConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class OracleConnectionOpenAsyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, CancellationToken cancellationToken)
    {
        if (instance is not null)
        {
            OracleConnectionIntegrationHelper.SetDatabaseOpenTelemetryTracing(instance);
        }

        return CallTargetState.GetDefault();
    }
}
