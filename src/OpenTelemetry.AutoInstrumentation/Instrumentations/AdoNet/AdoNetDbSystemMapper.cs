// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Instrumentation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

internal static class AdoNetDbSystemMapper
{
    private static readonly ConcurrentDictionary<Type, (bool Enabled, string SystemName, SqlProcessor.SqlDialect Dialect)> Cache = new();

    public static (bool Enabled, string SystemName, SqlProcessor.SqlDialect Dialect) GetInstrumentationDetails(Type commandType)
    {
        return Cache.GetOrAdd(commandType, static t => t.FullName switch
        {
            "Devart.Data.Oracle.OracleCommand" => (true, "oracle.db", SqlProcessor.SqlDialect.Oracle),
            "Devart.Data.MySql.MySqlCommand" => (true, "mysql", SqlProcessor.SqlDialect.MySql),
            "Devart.Data.PostgreSql.PgSqlCommand" => (true, "postgresql", SqlProcessor.SqlDialect.Default),
            "Microsoft.Data.SqlClient.SqlCommand" => (false, "microsoft.sql_server", SqlProcessor.SqlDialect.SqlServer),
            "Microsoft.Data.Sqlite.SqliteCommand" => (IsInstrumentationEnabled(TracerInstrumentation.Sqlite), "sqlite", SqlProcessor.SqlDialect.Sqlite),
            "MySqlConnector.MySqlCommand" => (false, "mysql", SqlProcessor.SqlDialect.MySql),
            "MySql.Data.MySqlClient.MySqlCommand" => (false, "mysql", SqlProcessor.SqlDialect.MySql),
            "Npgsql.NpgsqlCommand" => (false, "postgresql", SqlProcessor.SqlDialect.Default),
            "Oracle.ManagedDataAccess.Client.OracleCommand" => (false, "oracle.db", SqlProcessor.SqlDialect.Oracle),
            "System.Data.SqlClient.SqlCommand" => (false, "microsoft.sql_server", SqlProcessor.SqlDialect.SqlServer),
            "System.Data.SQLite.SQLiteCommand" => (false, "sqlite", SqlProcessor.SqlDialect.Sqlite),
            // Inherit provider handling for subclasses such as NpgsqlDataSourceCommand.
            _ => t.BaseType is { } baseType ? GetInstrumentationDetails(baseType) : (true, "other_sql", SqlProcessor.SqlDialect.Default)
        });
    }

    private static bool IsInstrumentationEnabled(TracerInstrumentation instrumentation)
    {
        return Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(instrumentation);
    }
}
