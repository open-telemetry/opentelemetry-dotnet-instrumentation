// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.SqlClient;

internal static class SqlClientTraceContextPropagator
{
    private const string SqlClientActivitySourceName = "OpenTelemetry.Instrumentation.SqlClient";
    private const string ContextInfoParameterName = "@opentelemetry_traceparent";
    private const string SetContextSql = $"set context_info {ContextInfoParameterName}";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<bool> IsEnabled = new(() => Instrumentation.TracerSettings.Value.InstrumentationOptions.SqlClientNetFxExperimentalContextPropagation);

    public static void Propagate<TTarget>(TTarget instance)
    {
        if (!IsEnabled.Value ||
            instance is not IDbCommand command ||
            command.CommandType != CommandType.Text ||
            string.Equals(command.CommandText, SetContextSql, StringComparison.OrdinalIgnoreCase) ||
            command.Connection is not { State: ConnectionState.Open } connection)
        {
            return;
        }

        var sqlActivity = Activity.Current;
        if (sqlActivity is null || sqlActivity.Source.Name != SqlClientActivitySourceName)
        {
            return;
        }

        try
        {
            using var setContextCommand = connection.CreateCommand();
            setContextCommand.Transaction = command.Transaction;
            setContextCommand.CommandText = SetContextSql;
            setContextCommand.CommandType = CommandType.Text;

            var traceparentParameter = setContextCommand.CreateParameter();
            traceparentParameter.ParameterName = ContextInfoParameterName;
            traceparentParameter.DbType = DbType.Binary;
            traceparentParameter.Value = Encoding.UTF8.GetBytes(FormatTraceParent(sqlActivity));
            _ = setContextCommand.Parameters.Add(traceparentParameter);

            var previousActivity = Activity.Current;
            using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
            try
            {
                Activity.Current = null;
                setContextCommand.ExecuteNonQuery();
            }
            finally
            {
                Activity.Current = previousActivity;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "SqlClient trace context propagation failed.");
        }
    }

    private static string FormatTraceParent(Activity activity)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "00-{0}-{1}-{2}",
            activity.TraceId,
            activity.SpanId,
            activity.ActivityTraceFlags.W3CFormatActivityTraceFlags());
    }
}
#endif
