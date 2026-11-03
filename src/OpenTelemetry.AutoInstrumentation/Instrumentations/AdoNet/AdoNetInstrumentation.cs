// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.Instrumentation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

internal static class AdoNetInstrumentation
{
    private static readonly ActivitySource Source =
        new(new ActivitySourceOptions("OpenTelemetry.AutoInstrumentation.AdoNet")
        {
            Version = AutoInstrumentationVersion.Version,
            TelemetrySchemaUrl = GenericAttributes.SchemaUrl1400
        });

    public static Activity? StartActivity<TTarget>(TTarget instance, string methodName)
    {
        if (instance is not IDbCommand iDbCommand)
        {
            return null;
        }

        // Suppress nested AdoNet spans (e.g., ExecuteDbDataReader called internally by ExecuteScalar)
        if (Activity.Current?.Source.Name == Source.Name)
        {
            return null;
        }

        var (enabled, systemName) = AdoNetDbSystemMapper.GetInstrumentationDetails(instance.GetType());

        if (!enabled)
        {
            return null;
        }

        var sqlStatementInfo = SqlProcessor.GetSanitizedSql(iDbCommand.CommandText);

        TagList tags = new()
        {
            { DatabaseAttributes.Keys.DbSystemName, systemName },
        };

        string activityName;
        if (string.IsNullOrEmpty(sqlStatementInfo.DbQuerySummary))
        {
            activityName = systemName;
        }
        else
        {
            activityName = sqlStatementInfo.DbQuerySummary;
            tags.Add(DatabaseAttributes.Keys.DbQuerySummary, sqlStatementInfo.DbQuerySummary);
        }

        if (!string.IsNullOrEmpty(sqlStatementInfo.SanitizedSql))
        {
            tags.Add(DatabaseAttributes.Keys.DbQueryText, sqlStatementInfo.SanitizedSql);
        }

        var databaseName = TryGetConnection(iDbCommand)?.Database;
        if (!string.IsNullOrEmpty(databaseName))
        {
            tags.Add(DatabaseAttributes.Keys.DbNamespace, databaseName);
        }

        if (iDbCommand is DbCommand dbCommand)
        {
            var dataSource = TryGetDbConnection(dbCommand)?.DataSource;
            if (!string.IsNullOrEmpty(dataSource))
            {
                var connectionDetails = SqlConnectionDetails.ParseFromDataSource(dataSource!);
                var serverAddress = connectionDetails.ServerHostName ?? connectionDetails.ServerIpAddress;

                if (!string.IsNullOrEmpty(serverAddress))
                {
                    tags.Add(NetworkAttributes.Keys.ServerAddress, serverAddress);

                    if (connectionDetails.BoxedPort is not null)
                    {
                        tags.Add(NetworkAttributes.Keys.ServerPort, connectionDetails.BoxedPort);
                    }
                }
            }
        }

        return Source.StartActivity(activityName, ActivityKind.Client, default(ActivityContext), tags);
    }

    // Commands created by DbDataSource throw NotSupportedException on Connection.
    // Skip the connection attributes instead of failing the call, so the activity still starts
    // and Activity.Current is not lost (see CallTarget EndMethodHandler).
    private static IDbConnection? TryGetConnection(IDbCommand command)
    {
        try
        {
            return command.Connection;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static DbConnection? TryGetDbConnection(DbCommand command)
    {
        try
        {
            return command.Connection;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    public static void StopActivity(Activity? activity, Exception? exception)
    {
        if (activity is null)
        {
            return;
        }

        if (exception is not null)
        {
            activity.SetException(exception);
            activity.SetTag(GenericAttributes.Keys.ErrorType, exception.GetType().FullName);
        }

        activity.Stop();
    }
}
