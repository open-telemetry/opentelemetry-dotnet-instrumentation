// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data;
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
        if (instance is not IDbCommand dbCommand)
        {
            return null;
        }

        var systemName = AdoNetDbSystemMapper.GetSystemName(instance.GetType());

        var sqlStatementInfo = SqlProcessor.GetSanitizedSql(dbCommand.CommandText);

        TagList tags = new()
        {
            { DatabaseAttributes.Keys.DbSystemName, systemName },
            { DatabaseAttributes.Keys.DbQuerySummary, sqlStatementInfo.DbQuerySummary },
            { DatabaseAttributes.Keys.DbQueryText, sqlStatementInfo.SanitizedSql },
        };

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
