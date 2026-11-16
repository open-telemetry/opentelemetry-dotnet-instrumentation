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

    [ThreadStatic]
    private static ActivityCapture? currentCapture;

    internal static bool IsPropagationEnabled => IsEnabled.Value;

    public static void Propagate<TTarget>(TTarget instance, ActivityContext context)
    {
        if (context.TraceId == default || context.SpanId == default ||
            instance is not IDbCommand command ||
            command.CommandType != CommandType.Text ||
            string.Equals(command.CommandText, SetContextSql, StringComparison.OrdinalIgnoreCase) ||
            command.Connection is not { State: ConnectionState.Open } connection)
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
            traceparentParameter.Value = Encoding.UTF8.GetBytes(FormatTraceParent(context));
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

    internal static ActivityListener CreateActivityListener()
    {
        // SqlClient restores Activity.Current before WriteBeginExecuteEvent returns.
        // Observe SDK-created activities without changing sampling or retaining the activity.
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == SqlClientActivitySourceName,
            ActivityStarted = static activity =>
            {
                if (currentCapture is { } capture)
                {
                    capture.Context = activity.Context;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    internal static ActivityCapture BeginCapture()
    {
        // WriteBeginExecuteEvent and its EventSource callbacks execute synchronously on the same thread.
        return currentCapture = new ActivityCapture(currentCapture);
    }

    internal static void EndCapture(ActivityCapture capture)
    {
        currentCapture = capture.Previous;
    }

    private static string FormatTraceParent(ActivityContext context)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "00-{0}-{1}-{2}",
            context.TraceId,
            context.SpanId,
            context.TraceFlags.W3CFormatActivityTraceFlags());
    }

    internal sealed class ActivityCapture(ActivityCapture? previous)
    {
        internal ActivityCapture? Previous { get; } = previous;

        internal ActivityContext Context { get; set; }
    }
}
#endif
