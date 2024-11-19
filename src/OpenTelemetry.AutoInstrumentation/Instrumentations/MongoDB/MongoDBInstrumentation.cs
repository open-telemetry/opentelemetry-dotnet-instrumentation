// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

internal static class MongoDBInstrumentation
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.MongoDB");

    public static Activity? StartDatabaseActivity(
        object? instance,
        string? serverAddress,
        int? serverPort,
        int? batchSize = null,
        string? statusCode = null)
    {
        if (instance == null)
        {
            return null;
        }

        var database = GetDatabaseName(instance);

        if (!TryGetQueryDetails(instance, database, out var collection, out var operationName))
        {
            return null;
        }

        var spanName = $"{operationName} {collection}";

        var activity = Source.StartActivity(name: spanName, kind: ActivityKind.Client);

        if (activity is { IsAllDataRequested: true })
        {
            SetCommonAttributes(activity, operationName, collection, database, serverAddress, serverPort, batchSize, statusCode);
        }

        return activity;
    }

    private static void SetCommonAttributes(
        Activity activity,
        string? operationName,
        string? collection,
        string? database,
        string? serverAddress,
        int? serverPort,
        int? batchSize,
        string? statusCode)
    {
        activity.SetTag(DatabaseAttributes.Keys.DbSystem, "mongodb");
        activity.SetTag(DatabaseAttributes.Keys.DbCollectionName, collection);
        activity.SetTag(DatabaseAttributes.Keys.DbNamespace, database);
        activity.SetTag(DatabaseAttributes.Keys.DbOperationName, operationName);

        activity.SetTag(NetworkAttributes.Keys.ServerAddress, serverAddress);
        activity.SetTag(NetworkAttributes.Keys.ServerPort, serverPort);

        activity.SetTag(DatabaseAttributes.Keys.DbQueryText, operationName);
        if (batchSize.HasValue)
        {
            activity.SetTag(DatabaseAttributes.Keys.DbOperationBatchSize, batchSize.Value);
        }

        if (!string.IsNullOrEmpty(statusCode))
        {
            activity.SetTag(DatabaseAttributes.Keys.DbResponseStatusCode, statusCode);
        }
    }

    private static string? GetDatabaseName(object wireProtocol)
    {
        if (wireProtocol.TryDuckCast<IWireProtocolWithDatabaseNamespaceStruct>(out var protocolWithDatabaseNamespace)
         && protocolWithDatabaseNamespace.DatabaseNamespace is not null
         && protocolWithDatabaseNamespace.DatabaseNamespace.TryDuckCast<DatabaseNamespaceStruct>(out var databaseNamespace))
        {
            return databaseNamespace.DatabaseName;
        }

        return null;
    }

    private static bool TryGetQueryDetails(object instance, string? databaseName, out string? collection, out string? operationName)
    {
        collection = null;
        operationName = null;

        if (instance.TryDuckCast<IWireProtocolWithCommandStruct>(out var protocolWithCommand)
         && protocolWithCommand.Command != null
         && protocolWithCommand.Command.TryDuckCast<IBsonDocumentProxy>(out var bsonDocument))
        {
            var firstElement = bsonDocument.GetElement(0);
            var mongoOperationName = firstElement.Name;

            if (mongoOperationName is "isMaster" or "hello")
            {
                return false;
            }

            collection = firstElement.Value?.ToString();
            operationName = mongoOperationName;
        }

        return true;
    }
}
