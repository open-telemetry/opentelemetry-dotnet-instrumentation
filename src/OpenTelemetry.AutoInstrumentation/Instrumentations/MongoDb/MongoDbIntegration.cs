using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using OpenTelemetry.AutoInstrumentation.Configuration;
using OpenTelemetry.AutoInstrumentation.Emit;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Tagging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDb
{
    /// <summary>
    /// Tracing integration for MongoDB.Driver.Core.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MongoDbIntegration
    {
        internal const string IntegrationName = nameof(Configuration.Instrumentation.MongoDb);

        internal static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);

        private const string OperationName = "mongodb.query";

        private static readonly ILogger Log = ConsoleLogger.Create(typeof(MongoDbIntegration));

        private static readonly ActivitySource ActivitySource = new ActivitySource(
            "OpenTelemetry.AutoInstrumentation.MongoDB", "0.0.1");

        internal static Activity CreateActivity(object wireProtocol, object connection)
        {
            var settings = Instrumentation.TracerSettings;

            if (!settings.IsIntegrationEnabled(IntegrationId))
            {
                // integration disabled, don't create a scope, skip this trace
                return null;
            }

            if (GetActiveMongoDbScope() != null)
            {
                // There is already a parent MongoDb span (nested calls)
                return null;
            }

            string databaseName = null;
            string host = null;
            string port = null;

            try
            {
                if (wireProtocol.TryGetFieldValue("_databaseNamespace", out object databaseNamespace))
                {
                    databaseNamespace?.TryGetPropertyValue("DatabaseName", out databaseName);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to access DatabaseName property.");
            }

            try
            {
                if (connection != null && connection.TryGetPropertyValue("EndPoint", out object endpoint))
                {
                    if (endpoint is IPEndPoint ipEndPoint)
                    {
                        host = ipEndPoint.Address.ToString();
                        port = ipEndPoint.Port.ToString();
                    }
                    else if (endpoint is DnsEndPoint dnsEndPoint)
                    {
                        host = dnsEndPoint.Host;
                        port = dnsEndPoint.Port.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to access EndPoint properties.");
            }

            string operationName = null;
            string collectionName = null;
            string query = null;
            string resourceName = null;

            try
            {
                if (wireProtocol.TryGetFieldValue("_command", out object command) && command != null)
                {
                    // the name of the first element in the command BsonDocument will be the operation type (insert, delete, find, etc)
                    // and its value is the collection name
                    if (command.TryCallMethod("GetElement", 0, out object firstElement) && firstElement != null)
                    {
                        firstElement.TryGetPropertyValue("Name", out operationName);

                        if (firstElement.TryGetPropertyValue("Value", out object collectionNameObj) && collectionNameObj != null)
                        {
                            collectionName = collectionNameObj.ToString();
                        }
                    }

                    query = command.ToString();

                    resourceName = $"{operationName ?? "operation"} {databaseName ?? "database"}";
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to access IWireProtocol.Command properties.");
            }

            Activity activity = null;

            try
            {
                var tags = new MongoDbTags
                {
                    DbName = databaseName,
                    Query = query,
                    Collection = collectionName,
                    Host = host,
                    Port = port
                };

                activity = ActivitySource.StartActivityWithTags(OperationName, tags);
                activity.SetResourceName(resourceName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating or populating scope.");
            }

            return activity;
        }

        private static Activity GetActiveMongoDbScope()
        {
            var parent = Activity.Current;

            if (parent != null &&
                parent.Source == ActivitySource &&
                parent.Tags.Any(x => x.Key == Tags.InstrumentationName))
            {
                return parent;
            }

            return null;
        }
    }
}
