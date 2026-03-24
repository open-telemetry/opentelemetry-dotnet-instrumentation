// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

internal static class AdoNetDbSystemMapper
{
    private static readonly ConcurrentDictionary<Type, (bool Enabled, string SystemName)> Cache = new();

    public static (bool Enabled, string SystemName) GetInstrumentationDetails(Type commandType)
    {
        return Cache.GetOrAdd(commandType, static t => t.FullName switch
        {
            "Devart.Data.Oracle.OracleCommand" => (true, "oracle"),
            "Devart.Data.MySql.MySqlCommand" => (true, "mysql"),
            "Devart.Data.PostgreSql.PgSqlCommand" => (true, "postgresql"),
            "Microsoft.Data.SqlClient.SqlCommand" => (false, "microsoft.sql_server"),
            "Microsoft.Data.Sqlite.SqliteCommand" => (IsInstrumentationEnabled(TracerInstrumentation.Sqlite), "sqlite"),
            "MySqlConnector.MySqlCommand" => (false, "mysql"),
            "MySql.Data.MySqlClient.MySqlCommand" => (false, "mysql"),
            "Npgsql.NpgsqlCommand" => (false, "postgresql"),
            "Oracle.ManagedDataAccess.Client" => (false, "oracle.db"),
            "System.Data.SqlClient.SqlCommand" => (false, "microsoft.sql_server"),
            "System.Data.SQLite.SQLiteCommand" => (false, "sqlite"),
            _ => (true, "other_sql")
        });
    }

    private static bool IsInstrumentationEnabled(TracerInstrumentation instrumentation)
    {
        return Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(instrumentation);
    }
}
