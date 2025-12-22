// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MySqlConnector;
using TestApplication.Shared;

namespace TestApplication.MySqlConnector;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var mySqlPort = GetMySqlPort(args);

        var connString = $@"Server=127.0.0.1;Port={mySqlPort};Uid=root";

        using var connection = new MySqlConnection(connString);
        await connection.OpenAsync().ConfigureAwait(false);

        using var cmd = new MySqlCommand(@"SELECT 123;", connection);
        using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            Console.WriteLine(reader.GetInt32(0));
        }
    }

    private static string GetMySqlPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "3306";
    }
}
