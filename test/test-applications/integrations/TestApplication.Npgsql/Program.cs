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

        var postgresPort = ArgumentHelper.GetArgument(args, "--postgres", "5432");

        var connString = $"Server=127.0.0.1;Port={postgresPort};User ID=postgres";

        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync().ConfigureAwait(false);

        using var cmd = new NpgsqlCommand(@"SELECT 123;", conn);
        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            Console.WriteLine(reader.GetInt32(0));
        }
    }
}
