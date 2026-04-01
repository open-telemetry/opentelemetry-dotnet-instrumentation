// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet.Integrations;

/// <summary>
/// ADO.NET DbCommand.ExecuteDbDataReaderAsync instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: AdoNetConstants.SystemDataCommonAssemblyName,
    typeName: AdoNetConstants.DbCommandTypeName,
    methodName: AdoNetConstants.ExecuteDbDataReaderAsyncMethodName,
    returnTypeName: AdoNetConstants.DbDataReaderTaskTypeName,
    parameterTypeNames: [AdoNetConstants.CommandBehaviorTypeName, ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.SystemDataCommonMinVersion,
    maximumVersion: AdoNetConstants.SystemDataCommonMaxVersion,
    integrationName: AdoNetConstants.IntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
[InstrumentMethod(
    assemblyName: AdoNetConstants.SystemDataAssemblyName,
    typeName: AdoNetConstants.DbCommandTypeName,
    methodName: AdoNetConstants.ExecuteDbDataReaderAsyncMethodName,
    returnTypeName: AdoNetConstants.DbDataReaderTaskTypeName,
    parameterTypeNames: [AdoNetConstants.CommandBehaviorTypeName, ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.SystemDataAsyncMinVersion,
    maximumVersion: AdoNetConstants.SystemDataMaxVersion,
    integrationName: AdoNetConstants.IntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
[InstrumentMethod(
    assemblyName: AdoNetConstants.NetStandardAssemblyName,
    typeName: AdoNetConstants.DbCommandTypeName,
    methodName: AdoNetConstants.ExecuteDbDataReaderAsyncMethodName,
    returnTypeName: AdoNetConstants.DbDataReaderTaskTypeName,
    parameterTypeNames: [AdoNetConstants.CommandBehaviorTypeName, ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.NetStandardMinVersion,
    maximumVersion: AdoNetConstants.NetStandardMaxVersion,
    integrationName: AdoNetConstants.IntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
[InstrumentMethod(
    assemblyName: AdoNetConstants.SqliteMicrosoft.AssemblyName,
    typeName: AdoNetConstants.SqliteMicrosoft.CommandTypeName,
    methodName: AdoNetConstants.ExecuteReaderAsyncMethodName,
    returnTypeName: AdoNetConstants.SqliteMicrosoft.DataReaderTaskTypeName,
    parameterTypeNames: [AdoNetConstants.CommandBehaviorTypeName, ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.SqliteMicrosoft.MinVersion,
    maximumVersion: AdoNetConstants.SqliteMicrosoft.MaxVersion,
    integrationName: AdoNetConstants.SqliteMicrosoft.SqliteIntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Direct)]
public static class CommandExecuteDbDataReaderAsyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, CommandBehavior behavior, CancellationToken cancellationToken)
    {
        var activity = AdoNetInstrumentation.StartActivity(instance, AdoNetConstants.ExecuteDbDataReaderAsyncMethodName);
        return new CallTargetState(activity, null);
    }

    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        AdoNetInstrumentation.StopActivity(state.Activity, exception);
        return returnValue;
    }
}
