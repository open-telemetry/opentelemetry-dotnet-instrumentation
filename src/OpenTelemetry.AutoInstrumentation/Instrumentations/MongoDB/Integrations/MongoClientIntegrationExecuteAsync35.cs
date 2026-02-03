// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.Integrations;

/// <summary>
/// MongoDB.Driver.MongoClient calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandUsingQueryMessageWireProtocol`1",
    methodName: "ExecuteAsync",
    returnTypeName: ClrNames.GenericTaskWithGenericClassParameter,
    parameterTypeNames: ["MongoDB.Driver.OperationContext", "MongoDB.Driver.Core.Connections.IConnection"],
    minimumVersion: MongoDBConstants.MinimumVersion35,
    maximumVersion: MongoDBConstants.MaximumVersion35,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandUsingCommandMessageWireProtocol`1",
    methodName: "ExecuteAsync",
    returnTypeName: ClrNames.GenericTaskWithGenericClassParameter,
    parameterTypeNames: ["MongoDB.Driver.OperationContext", "MongoDB.Driver.Core.Connections.IConnection"],
    minimumVersion: MongoDBConstants.MinimumVersion35,
    maximumVersion: MongoDBConstants.MaximumVersion35,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.QueryWireProtocol`1",
    methodName: "ExecuteAsync",
    returnTypeName: ClrNames.GenericTaskWithGenericClassParameter,
    parameterTypeNames: ["MongoDB.Driver.OperationContext", "MongoDB.Driver.Core.Connections.IConnection"],
    minimumVersion: MongoDBConstants.MinimumVersion35,
    maximumVersion: MongoDBConstants.MaximumVersion35,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandWireProtocol`1",
    methodName: "ExecuteAsync",
    returnTypeName: ClrNames.GenericTaskWithGenericClassParameter,
    parameterTypeNames: ["MongoDB.Driver.OperationContext", "MongoDB.Driver.Core.Connections.IConnection"],
    minimumVersion: MongoDBConstants.MinimumVersion35,
    maximumVersion: MongoDBConstants.MaximumVersion35,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class MongoClientIntegrationExecuteAsync35
{
    internal static CallTargetState OnMethodBegin<TTarget, TOperationContext, TConnection>(TTarget instance, TOperationContext? operationContext, TConnection connection)
        where TConnection : IConnection
    {
        var activity = MongoDBInstrumentation.StartDatabaseActivity(instance, connection);
        return new CallTargetState(activity, null);
    }

    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
        var activity = state.Activity;

        if (activity is null)
        {
            return returnValue;
        }

        if (exception is not null)
        {
            MongoDBInstrumentation.OnError(activity, exception);
        }

        activity.Stop();
        return returnValue;
    }
}
