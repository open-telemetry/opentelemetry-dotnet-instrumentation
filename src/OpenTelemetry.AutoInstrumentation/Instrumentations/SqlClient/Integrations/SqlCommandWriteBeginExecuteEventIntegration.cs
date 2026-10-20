// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.SqlClient.Integrations;

/// <summary>
/// System.Data.SqlClient and Microsoft.Data.SqlClient WriteBeginExecuteEvent instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: SqlClientConstants.SystemDataAssemblyName,
    typeName: SqlClientConstants.SystemDataSqlCommandTypeName,
    methodName: SqlClientConstants.WriteBeginExecuteEventMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [],
    minimumVersion: SqlClientConstants.SystemDataMinVersion,
    maximumVersion: SqlClientConstants.SystemDataMaxVersion,
    integrationName: SqlClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: SqlClientConstants.SystemDataSqlClientAssemblyName,
    typeName: SqlClientConstants.SystemDataSqlCommandTypeName,
    methodName: SqlClientConstants.WriteBeginExecuteEventMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [],
    minimumVersion: SqlClientConstants.SystemDataSqlClientMinVersion,
    maximumVersion: SqlClientConstants.SystemDataSqlClientMaxVersion,
    integrationName: SqlClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: SqlClientConstants.MicrosoftDataSqlClientAssemblyName,
    typeName: SqlClientConstants.MicrosoftDataSqlCommandTypeName,
    methodName: SqlClientConstants.WriteBeginExecuteEventMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [],
    minimumVersion: SqlClientConstants.MicrosoftDataSqlClientMinVersion,
    maximumVersion: SqlClientConstants.MicrosoftDataSqlClientMaxVersion,
    integrationName: SqlClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class SqlCommandWriteBeginExecuteEventIntegration
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is null)
        {
            SqlClientTraceContextPropagator.Propagate(instance);
        }

        return CallTargetReturn.GetDefault();
    }
}
#endif
