// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;

namespace TestApplication.SqlClient.System;
#pragma warning disable CS0618 // Type or member is obsolete, System.Data.SqlClient classes are deprecated in 4.9.0+

/// <summary>
/// Runs a query through Entity Framework 6, which wraps every provider command
/// in its internal InterceptableDbCommand before executing it.
/// </summary>
internal static class EntityFramework6Scenario
{
    public const string Argument = "--entity-framework-6";

    public static void Run(string connectionString)
    {
        DbConfiguration.SetConfiguration(new SqlClientConfiguration());
        Database.SetInitializer<DbContext>(null);

        using var context = new DbContext(connectionString);
        var result = context.Database.SqlQuery<int>("SELECT 1").Single();
        Console.WriteLine($"Entity Framework 6 query result: {result}");
    }

    private sealed class SqlClientConfiguration : DbConfiguration
    {
        public SqlClientConfiguration()
        {
            SetProviderFactory("System.Data.SqlClient", SqlClientFactory.Instance);
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
