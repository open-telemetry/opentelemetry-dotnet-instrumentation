// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Npgsql;
using TestApplication.Shared;

namespace TestApplication.Npgsql;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var postgresPort = GetNpgsqlPort(args);

        var connString = $"Server=127.0.0.1;Port={postgresPort};User ID=postgres";

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync().ConfigureAwait(false);

        using var cmd = new NpgsqlCommand(@"SELECT 123;", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            Console.WriteLine(reader.GetInt32(0));
        }
    }

    private static string GetNpgsqlPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "5432";
    }
}
