// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet.Integrations;

/// <summary>
/// ADO.NET DbCommand.ExecuteScalarAsync instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: AdoNetConstants.SystemDataCommonAssemblyName,
    typeName: AdoNetConstants.DbCommandTypeName,
    methodName: AdoNetConstants.ExecuteScalarAsyncMethodName,
    returnTypeName: ClrNames.ObjectTask,
    parameterTypeNames: [ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.SystemDataCommonMinVersion,
    maximumVersion: AdoNetConstants.SystemDataCommonMaxVersion,
    integrationName: AdoNetConstants.IntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
[InstrumentMethod(
    assemblyName: AdoNetConstants.SystemDataAssemblyName,
    typeName: AdoNetConstants.DbCommandTypeName,
    methodName: AdoNetConstants.ExecuteScalarAsyncMethodName,
    returnTypeName: ClrNames.ObjectTask,
    parameterTypeNames: [ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.SystemDataAsyncMinVersion,
    maximumVersion: AdoNetConstants.SystemDataMaxVersion,
    integrationName: AdoNetConstants.IntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
[InstrumentMethod(
    assemblyName: AdoNetConstants.NetStandardAssemblyName,
    typeName: AdoNetConstants.DbCommandTypeName,
    methodName: AdoNetConstants.ExecuteScalarAsyncMethodName,
    returnTypeName: ClrNames.ObjectTask,
    parameterTypeNames: [ClrNames.CancellationToken],
    minimumVersion: AdoNetConstants.NetStandardMinVersion,
    maximumVersion: AdoNetConstants.NetStandardMaxVersion,
    integrationName: AdoNetConstants.IntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
public static class CommandExecuteScalarAsyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, CancellationToken cancellationToken)
    {
        var activity = AdoNetInstrumentation.StartActivity(instance, AdoNetConstants.ExecuteScalarAsyncMethodName);
        return new CallTargetState(activity, null);
    }

    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        AdoNetInstrumentation.StopActivity(state.Activity, exception);
        return returnValue;
    }
}
