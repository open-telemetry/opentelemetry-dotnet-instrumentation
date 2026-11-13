// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Data.SqlClient;
using TestApplication.Shared;

namespace TestApplication.AdoNetEntityFramework6;
#pragma warning disable CS0618 // Type or member is obsolete, System.Data.SqlClient classes are deprecated in 4.9.0+

/// <summary>
/// Runs a query through Entity Framework 6, which wraps every provider command
/// in its internal InterceptableDbCommand before executing it.
/// </summary>
internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var databasePassword = EnvironmentHelper.GetRequiredEnvironmentVariable("TEST_DATABASE_PASSWORD");
        var databasePort = ArgumentHelper.GetRequiredArgument(args, "--port");
        var connectionString = $"Server=127.0.0.1,{databasePort};User=sa;Password={databasePassword};TrustServerCertificate=True;";

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
