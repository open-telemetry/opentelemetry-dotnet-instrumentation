// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.OracleMda.DuckTypes;

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
public static class OracleConnectionConstructorIntegration
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is null && instance.TryDuckCast<IOracleConnection>(out var oracleConnection))
        {
            oracleConnection.DatabaseOpenTelemetryTracing = true;
        }

        return CallTargetReturn.GetDefault();
    }
}
