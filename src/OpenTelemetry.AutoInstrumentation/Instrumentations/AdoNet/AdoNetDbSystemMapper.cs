// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.AdoNet;

internal static class AdoNetDbSystemMapper
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    public static string GetSystemName(Type commandType)
    {
        return Cache.GetOrAdd(commandType, static t => t.FullName switch
        {
            // TODO add here more mappings for other ADO.NET providers
            "Devart.Data.Oracle.OracleCommand" => "oracle",
            "Devart.Data.MySql.MySqlCommand" => "mysql",
            "Devart.Data.PostgreSql.PgSqlCommand" => "postgresql",
            _ => "other_sql"
        });
    }
}
