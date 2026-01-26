// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

internal static class MongoDBInstrumentation
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.MongoDB", AutoInstrumentationVersion.Version);

    public static Activity? StartDatabaseActivity(
        object? instance,
        IConnection connection)
    {
        if (instance == null)
        {
            return null;
        }

        if (!TryGetDatabaseName(instance, out var database))
        {
            return null;
        }

        if (!TryGetQueryDetails(instance, out var collection, out var operationName))
        {
            return null;
        }

        if (!TryGetNetworkAttributes(connection, out var serverAddress, out var serverName, out var serverPort))
        {
            return null;
        }

        var spanName = $"{operationName} {collection}";

        var activity = Source.StartActivity(name: spanName, kind: ActivityKind.Client);

        if (activity is { IsAllDataRequested: true })
        {
            SetCommonAttributes(activity, operationName, collection, database, serverAddress, serverName, serverPort);
        }

        return activity;
    }

    private static void SetCommonAttributes(
        Activity activity,
        string? operationName,
        string? collection,
        string? database,
        string? serverAddress,
        string? serverName,
        int? serverPort)
    {
        activity.SetTag(DatabaseAttributes.Keys.DbSystem, DatabaseAttributes.Values.MongoDB.MongoDbSystem);
        activity.SetTag(DatabaseAttributes.Keys.DbCollectionName, collection);
        activity.SetTag(DatabaseAttributes.Keys.DbNamespace, database);
        activity.SetTag(DatabaseAttributes.Keys.DbOperationName, operationName);

        if (!string.IsNullOrEmpty(serverAddress))
        {
            activity.SetTag(NetworkAttributes.Keys.NetworkPeerAddress, serverAddress);
        }

        if (!string.IsNullOrEmpty(serverName))
        {
            activity.SetTag(NetworkAttributes.Keys.ServerAddress, serverName);
        }

        if (serverPort is not null)
        {
            activity.SetTag(NetworkAttributes.Keys.ServerPort, serverPort);
            activity.SetTag(NetworkAttributes.Keys.NetworkPeerPort, serverPort);
        }
    }

    private static bool TryGetDatabaseName(object instance, out string? database)
    {
        database = null;
        if (instance.TryDuckCast<IWireProtocolWithDatabaseNamespaceStruct>(out var protocolWithDatabaseNamespace)
         && protocolWithDatabaseNamespace.DatabaseNamespace is not null
         && protocolWithDatabaseNamespace.DatabaseNamespace.TryDuckCast<DatabaseNamespaceStruct>(out var databaseNamespace))
        {
            database = databaseNamespace.DatabaseName;
        }

        if (string.IsNullOrEmpty(database))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetQueryDetails(object instance, out string? collection, out string? operationName)
    {
        collection = null;
        operationName = null;

        if (instance.TryDuckCast<IWireProtocolWithCommandStruct>(out var protocolWithCommand)
         && protocolWithCommand.Command is not null
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

        if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(operationName))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetNetworkAttributes(IConnection connection, out string? serverAddress, out string? serverName, out int? serverPort)
    {
        serverAddress = null;
        serverName = null;
        serverPort = null;

        if (connection.EndPoint == null)
        {
            return false;
        }

        if (connection.EndPoint is IPEndPoint ipEndPoint)
        {
            serverAddress = ipEndPoint.Address.ToString();
            serverPort = ipEndPoint.Port;

            serverName = Dns.GetHostEntry(serverAddress).ToString();
        }
        else if (connection.EndPoint is DnsEndPoint dnsEndPoint)
        {
            serverName = dnsEndPoint.Host;
            serverPort = dnsEndPoint.Port;

            var ipAddresses = Dns.GetHostAddresses(serverName);
            if (ipAddresses.Length > 0)
            {
                serverAddress = ipAddresses[0].ToString();
            }
        }

        return true;
    }
}
