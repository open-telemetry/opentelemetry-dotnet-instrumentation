// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MongoDB;

internal static class MongoDBInstrumentation
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.MongoDB");

    public static Activity? StartDatabaseActivity(
        string operationName,
        string collection,
        string database,
        string? serverAddress,
        int? serverPort,
        int? batchSize = null,
        string? statusCode = null)
    {
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
        string operationName,
        string collection,
        string database,
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
}
