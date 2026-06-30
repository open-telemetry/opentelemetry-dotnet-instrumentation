// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda.Integrations;

/// <summary>
/// OracleConnection constructor instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: OracleMdaConstants.OracleMdaAssemblyName,
    typeName: OracleMdaConstants.OracleConnectionTypeName,
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [],
    minimumVersion: OracleMdaConstants.MinVersion,
    maximumVersion: OracleMdaConstants.MaxVersion,
    integrationName: OracleMdaConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: OracleMdaConstants.OracleMdaAssemblyName,
    typeName: OracleMdaConstants.OracleConnectionTypeName,
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String],
    minimumVersion: OracleMdaConstants.MinVersion,
    maximumVersion: OracleMdaConstants.MaxVersion,
    integrationName: OracleMdaConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class OracleConnectionConstructorIntegration
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is null && instance is not null)
        {
            OracleConnectionIntegrationHelper.SetDatabaseOpenTelemetryTracing(instance);
        }

        return CallTargetReturn.GetDefault();
    }
}
