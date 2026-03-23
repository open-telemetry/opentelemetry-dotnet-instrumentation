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

        var databaseName = iDbCommand.Connection?.Database;

        var systemName = AdoNetDbSystemMapper.GetSystemName(instance.GetType());

        var sqlStatementInfo = SqlProcessor.GetSanitizedSql(iDbCommand.CommandText);

        TagList tags = new()
        {
            { DatabaseAttributes.Keys.DbSystemName, systemName },
            { DatabaseAttributes.Keys.DbQuerySummary, sqlStatementInfo.DbQuerySummary },
            { DatabaseAttributes.Keys.DbQueryText, sqlStatementInfo.SanitizedSql },
        };

        if (!string.IsNullOrEmpty(databaseName))
        {
            tags.Add(DatabaseAttributes.Keys.DbNamespace, databaseName);
        }

        if (iDbCommand is DbCommand dbCommand)
        {
            var dataSource = dbCommand.Connection?.DataSource;
            if (!string.IsNullOrEmpty(dataSource))
            {
                var connectionDetails = SqlConnectionDetails.ParseFromDataSource(dataSource!);
                var serverAddress = connectionDetails.ServerHostName ?? connectionDetails.ServerIpAddress;

                if (!string.IsNullOrEmpty(serverAddress))
                {
                    tags.Add(NetworkAttributes.Keys.ServerAddress, serverAddress);

                    if (connectionDetails.Port.HasValue)
                    {
                        tags.Add(NetworkAttributes.Keys.ServerPort, connectionDetails.Port.Value);
                    }
                }
            }
        }

        return Source.StartActivity(sqlStatementInfo.DbQuerySummary, ActivityKind.Client, default(ActivityContext), tags);
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
