// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql;

internal static class NpgsqlTraceContextPropagator
{
    private const byte FailedTransactionStatus = (byte)'E';

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<bool> IsEnabled = new(() => Instrumentation.TracerSettings.Value.InstrumentationOptions.NpgsqlContextPropagation);
    private static readonly ConcurrentDictionary<Type, MemberInfo> CurrentActivityMembers = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo> ActivityIdProperties = new();
    private static readonly ConditionalWeakTable<object, ConnectorContextState> ConnectorStates = new();

    private static int _multiplexingWarningLogged;

    public static void Propagate(object? command, object? connector)
    {
        if (!IsEnabled.Value || command is null || connector is null)
        {
            return;
        }

        try
        {
            PropagateActivityCore(GetCurrentActivity(command), connector);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Npgsql trace context propagation failed.");
        }
    }

    public static void PropagateCopyActivity(object? activity, object? connector)
    {
        if (!IsEnabled.Value || connector is null)
        {
            return;
        }

        try
        {
            PropagateActivityCore(activity, connector);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Npgsql trace context propagation failed.");
        }
    }

    public static void ClearOnUserActionEnd(object? connector)
    {
        if (!IsEnabled.Value || connector is null)
        {
            return;
        }

        try
        {
            if (!ConnectorStates.TryGetValue(connector, out var state))
            {
                return;
            }

            lock (state)
            {
                ResetApplicationName(connector, state);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Npgsql trace context cleanup at the end of a user action failed.");
        }
    }

    private static void PropagateActivityCore(object? activity, object connector)
    {
        var npgsqlConnector = connector.DuckCast<INpgsqlConnector>();
        if (npgsqlConnector is null)
        {
            return;
        }

        if (npgsqlConnector.Settings.Multiplexing)
        {
            if (Interlocked.Exchange(ref _multiplexingWarningLogged, 1) == 0)
            {
                Logger.Warning("Npgsql trace context propagation is disabled for multiplexed connections because PostgreSQL application_name is connection-scoped.");
            }

            return;
        }

        var traceParent = GetActivityId(activity);
        var state = ConnectorStates.GetValue(connector, static _ => new ConnectorContextState());
        lock (state)
        {
            // PostgreSQL rejects every command except transaction recovery while a transaction is
            // failed. Leave the state intact so rollback can run, then clean it when recovery completes.
            if (npgsqlConnector.TransactionStatus == FailedTransactionStatus)
            {
                return;
            }

            if (traceParent is not null && IsW3CTraceParent(traceParent))
            {
                ExecuteApplicationNameCommand(npgsqlConnector, $"SET application_name = '{traceParent}'");
                state.HasPropagatedContext = true;
                return;
            }

            ResetApplicationName(npgsqlConnector, state);
        }
    }

    private static void ResetApplicationName(object connector, ConnectorContextState state)
    {
        var npgsqlConnector = connector.DuckCast<INpgsqlConnector>();
        if (npgsqlConnector is not null)
        {
            ResetApplicationName(npgsqlConnector, state);
        }
    }

    private static void ResetApplicationName(INpgsqlConnector connector, ConnectorContextState state)
    {
        if (!state.HasPropagatedContext || connector.TransactionStatus == FailedTransactionStatus)
        {
            return;
        }

        ExecuteApplicationNameCommand(connector, "RESET application_name");
        state.HasPropagatedContext = false;
    }

    private static void ExecuteApplicationNameCommand(INpgsqlConnector connector, string command)
    {
        // Complete propagation before Npgsql writes the application request, or cleanup while Npgsql
        // still owns the connector at the end of the user action. This deliberately adds a separate
        // round trip.
        connector.ExecuteInternalCommand(command);
    }

    private static object? GetCurrentActivity(object command)
    {
        var commandType = command.GetType();
        if (!CurrentActivityMembers.TryGetValue(commandType, out var currentActivityMember))
        {
            currentActivityMember = commandType.GetProperty("CurrentActivity", BindingFlags.Instance | BindingFlags.NonPublic) as MemberInfo
                                    ?? commandType.GetField("CurrentActivity", BindingFlags.Instance | BindingFlags.NonPublic);
            if (currentActivityMember is null)
            {
                return null;
            }

            CurrentActivityMembers.TryAdd(commandType, currentActivityMember);
        }

        return currentActivityMember switch
        {
            PropertyInfo property => property.GetValue(command),
            FieldInfo field => field.GetValue(command),
            _ => null
        };
    }

    private static string? GetActivityId(object? activity)
    {
        if (activity is null)
        {
            return null;
        }

        var activityType = activity.GetType();
        if (!ActivityIdProperties.TryGetValue(activityType, out var activityIdProperty))
        {
            activityIdProperty = activityType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public);
            if (activityIdProperty is null)
            {
                return null;
            }

            ActivityIdProperties.TryAdd(activityType, activityIdProperty);
        }

        return activityIdProperty.GetValue(activity) as string;
    }

    private static bool IsW3CTraceParent(string value)
    {
        if (value.Length != 55 ||
            value[0] != '0' ||
            value[1] != '0' ||
            value[2] != '-' ||
            value[35] != '-' ||
            value[52] != '-')
        {
            return false;
        }

        for (var i = 3; i < value.Length; i++)
        {
            if (i is 35 or 52)
            {
                continue;
            }

            var character = value[i];
            if (character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ConnectorContextState
    {
        public bool HasPropagatedContext { get; set; }
    }
}
