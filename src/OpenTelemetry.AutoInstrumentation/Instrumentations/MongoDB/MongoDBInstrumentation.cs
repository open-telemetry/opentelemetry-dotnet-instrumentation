// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

internal static class MongoDBInstrumentation
{
    private static readonly ActivitySource Source = CreateActivitySource();

    public static Activity? StartDatabaseActivity(
        object? instance,
        IConnection connection)
    {
        if (instance == null)
        {
            return null;
        }

        string? database = null;
        if (TryGetDatabaseName(instance, out var dbName))
        {
            database = dbName;
        }

        if (!TryGetQueryDetails(instance, out var collection, out var operationName, out var batchSize))
        {
            return null;
        }

        if (!TryGetNetworkAttributes(connection, out var serverAddress, out var serverName, out var serverPort))
        {
            return null;
        }

        var tags = new ActivityTagsCollection
        {
            { DatabaseAttributes.Keys.DbSystemName, DatabaseAttributes.Values.MongoDB.MongoDbSystem },
            { DatabaseAttributes.Keys.DbCollectionName, collection },
            { DatabaseAttributes.Keys.DbOperationName, operationName },
        };

        if (database != null)
        {
            tags.Add(DatabaseAttributes.Keys.DbNamespace, database);
        }

        if (batchSize.HasValue)
        {
            tags.Add(DatabaseAttributes.Keys.DbOperationBatchSize, batchSize.Value);
        }

        if (!string.IsNullOrEmpty(serverAddress))
        {
            tags.Add(NetworkAttributes.Keys.ServerAddress, serverAddress);
        }
        else if (!string.IsNullOrEmpty(serverName))
        {
            tags.Add(NetworkAttributes.Keys.ServerAddress, serverName);
        }

        if (serverPort is not null)
        {
            tags.Add(NetworkAttributes.Keys.ServerPort, serverPort);
        }

        var spanName = $"{operationName} {collection}";

        return Source.StartActivity(spanName, ActivityKind.Client, default(ActivityContext), tags);
    }

    private static ActivitySource CreateActivitySource()
    {
        var assemblyName = typeof(MongoDBInstrumentation).Assembly.GetName();
        var version = assemblyName.Version?.ToString();
        var activitySourceOptions = new ActivitySourceOptions(assemblyName.Name!)
        {
            Version = version,
            TelemetrySchemaUrl = "https://opentelemetry.io/schemas/1.39.0",
        };

        return new ActivitySource(activitySourceOptions);
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

    private static bool TryGetQueryDetails(object instance, out string? collection, out string? operationName, out int? batchSize)
    {
        collection = null;
        operationName = null;
        batchSize = null;

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

            if (operationName == "insert" || operationName == "update" || operationName == "delete")
            {
                var targetField = operationName switch
                {
                    "insert" => "documents",
                    "update" => "updates",
                    "delete" => "deletes",
                    _ => null
                };

                if (targetField != null)
                {
                    for (var i = 1; i < bsonDocument.ElementCount; i++)
                    {
                        var element = bsonDocument.GetElement(i);
                        if (element.Name == targetField && element.Value.TryDuckCast<IBsonArrayProxy>(out var bsonArray))
                        {
                            batchSize = bsonArray.Count;
                            break;
                        }
                    }
                }
            }
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

    internal static void OnError(Activity activity, Exception exception)
    {
        activity.SetException(exception);

        if (exception.GetType().Name.Equals("MongoCommandException", StringComparison.Ordinal))
        {
            try
            {
                // Reflection to get the Code property from MongoCommandException
                var propertyInfo = exception.GetType().GetProperty("Code");
                if (propertyInfo != null)
                {
                    var code = propertyInfo.GetValue(exception);
                    if (code != null)
                    {
                        activity.SetTag("db.response.status_code", code.ToString());
                    }
                }
            }
            catch
            {
                // accessing the property failed, ignore
            }
        }
    }
}
