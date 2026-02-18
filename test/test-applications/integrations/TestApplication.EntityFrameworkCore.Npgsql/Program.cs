// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TestApplication.Shared;

namespace TestApplication.EntityFrameworkCore.Npgsql;

internal static class Program
{
    private static readonly ActivitySource ActivitySource = new("TestApplication.EntityFrameworkCore.Npgsql");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var postgresPort = GetPostgresPort(args);
        var connectionString = $"Server=127.0.0.1;Port={postgresPort};User ID=postgres;Database=postgres";

        var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var activity = ActivitySource.StartActivity("parent");
        using var context = new TestDbContext(contextOptions);

        await context.Database.ExecuteSqlRawAsync("SELECT 123;").ConfigureAwait(false);
    }

    private static string GetPostgresPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "5432";
    }
}
