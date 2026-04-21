// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.EntityFrameworkCore;
using Npgsql;
using TestApplication.Shared;

namespace TestApplication.EntityFrameworkCore.Npgsql;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var postgresPort = GetPostgresPort(args);
        var connectionString = $"Server=127.0.0.1;Port={postgresPort};User ID=postgres;Database=postgres";

        var contextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using (var context = new TestDbContext(contextOptions))
        {
            await context.Database.ExecuteSqlRawAsync("SELECT 123;").ConfigureAwait(false);
        }

        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync().ConfigureAwait(false);

            var command = new NpgsqlCommand("SELECT 456;", connection);
            try
            {
                var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                try
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        Console.WriteLine(reader.GetInt32(0));
                    }
                }
                finally
                {
                    await reader.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await command.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
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
