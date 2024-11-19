// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.Integrations;

/// <summary>
/// MongoDB.Driver.MongoClient calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: MongoDBConstants.AssemblyName,
    typeName: MongoDBConstants.TypeName,
    methodName: "Execute",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "MongoDB.Driver.Core.Connections.IConnection", ClrNames.CancellationToken },
    minimumVersion: MongoDBConstants.MinimumVersion,
    maximumVersion: MongoDBConstants.MaximumVersion,
    integrationName: MongoDBConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class MongoClientIntegrationExecute
{
    internal static CallTargetState OnMethodBegin<TTarget, TConnection>(TTarget instance, TConnection connection, CancellationToken cancellationToken)
        where TConnection : IConnection
    {
        var scope = MongoDBInstrumentation.StartDatabaseActivity(instance, connection.Endpoint!.HostName, connection.Endpoint!.Port);

        if (scope == null)
        {
            return CallTargetState.GetDefault();
        }

        return new CallTargetState(scope);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    {
        return CallTargetReturn.GetDefault();
    }
}
