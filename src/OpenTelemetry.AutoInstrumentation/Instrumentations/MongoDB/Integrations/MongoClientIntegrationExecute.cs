// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.Integrations;

/// <summary>
/// MongoDB.Driver.MongoClient calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandUsingQueryMessageWireProtocol`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion3,
    maximumVersion: MongoDBConstants.MaximumVersion3,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandUsingCommandMessageWireProtocol`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion3,
    maximumVersion: MongoDBConstants.MaximumVersion3,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.QueryWireProtocol`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion3,
    maximumVersion: MongoDBConstants.MaximumVersion3,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName3,
    typeName: "MongoDB.Driver.Core.WireProtocol.WriteWireProtocolBase`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion3,
    maximumVersion: MongoDBConstants.MaximumVersion3,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandUsingQueryMessageWireProtocol`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion,
    maximumVersion: MongoDBConstants.MaximumVersion,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName,
    typeName: "MongoDB.Driver.Core.WireProtocol.CommandUsingCommandMessageWireProtocol`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion,
    maximumVersion: MongoDBConstants.MaximumVersion,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName,
    typeName: "MongoDB.Driver.Core.WireProtocol.QueryWireProtocol`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion,
    maximumVersion: MongoDBConstants.MaximumVersion,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName,
    typeName: "MongoDB.Driver.Core.WireProtocol.WriteWireProtocolBase`1",
    methodName: "Execute",
    returnTypeName: "!0",
    parameterTypeNames: ["MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken],
    minimumVersion: MongoDBConstants.MinimumVersion,
    maximumVersion: MongoDBConstants.MaximumVersion,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class MongoClientIntegrationExecute
{
    internal static CallTargetState OnMethodBegin<TTarget, TConnection>(
        TTarget instance,
        TConnection connection,
        CancellationToken cancellationToken)
        where TConnection : IConnection
    {
        var activity = MongoDBInstrumentation.StartDatabaseActivity(instance, connection);
        return new CallTargetState(activity, null);
    }

    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(
        TTarget instance,
        TReturn returnValue,
        Exception exception,
        in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return new CallTargetReturn<TReturn>(returnValue);
        }

        if (exception is not null)
        {
            MongoDBInstrumentation.OnError(activity, exception);
        }

        activity.Stop();
        return new CallTargetReturn<TReturn>(returnValue);
    }
}
