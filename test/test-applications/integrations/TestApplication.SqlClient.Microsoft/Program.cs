// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Data.SqlClient;
using TestApplication.Shared;

namespace TestApplication.SqlClient.Microsoft;

/// <summary>
/// This test application uses SqlConnection from Microsoft.Data.SqlClient (Nuget package).
/// </summary>
internal static class Program
{
    private const string CreateCommand = "CREATE TABLE MY_TABLE ( Id int, Value1 varchar(255), Value2 varchar(255) )";
    private const string DropCommand = "DROP TABLE MY_TABLE";
    private const string InsertCommand = "INSERT INTO MY_TABLE VALUES ( 1, 'value1', 'value2' )";
    private const string SelectCommand = "SELECT * FROM MY_TABLE";

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var (databasePassword, databasePort) = ParseArgs(args);
        var connectionString = GetConnectionString(databasePassword, databasePort);

        using (var connection = new SqlConnection(connectionString))
        {
            ExecuteCommands(connection);
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await ExecuteAsyncCommands(connection).ConfigureAwait(false);
        }

        // The "LONG_RUNNING" environment variable is used by tests that access/receive
        // data that takes time to be produced.
        var longRunning = Environment.GetEnvironmentVariable("LONG_RUNNING");
        if (longRunning == "true")
        {
            // In this case it is necessary to ensure that the test has a chance to read the
            // expected data, only by keeping the application alive for some time that can
            // be ensured. Anyway, tests that set "LONG_RUNNING" env var to true are expected
            // to kill the process directly.
            Console.WriteLine("LONG_RUNNING is true, waiting for process to be killed...");
#if NET
            await Process.GetCurrentProcess().WaitForExitAsync().ConfigureAwait(false);
#else
            Process.GetCurrentProcess().WaitForExit();
#endif
        }
    }

    private static void ExecuteCommands(SqlConnection connection)
    {
        connection.Open();
        ExecuteCreate(connection);
        ExecuteInsert(connection);
        ExecuteSelect(connection);
        ExecuteDrop(connection);
    }

    private static void ExecuteCreate(SqlConnection connection)
    {
        ExecuteCommand(CreateCommand, connection);
    }

    private static void ExecuteInsert(SqlConnection connection)
    {
        ExecuteCommand(InsertCommand, connection);
    }

    private static void ExecuteSelect(SqlConnection connection)
    {
        ExecuteCommand(SelectCommand, connection);
    }

    private static void ExecuteDrop(SqlConnection connection)
    {
        ExecuteCommand(DropCommand, connection);
    }

    private static void ExecuteCommand(string commandString, SqlConnection connection)
    {
        try
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities. All queries are static strings.
            using var command = new SqlCommand(commandString, connection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities. All queries are static strings.
            using var reader = command.ExecuteReader();
            Console.WriteLine($"SQL query executed successfully: {commandString}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while executing SQL query: {commandString}.\n{ex.Message}");
        }
    }

    private static async Task ExecuteAsyncCommands(SqlConnection connection)
    {
        await connection.OpenAsync().ConfigureAwait(false);
        await ExecuteCreateAsync(connection).ConfigureAwait(false);
        await ExecuteInsertAsync(connection).ConfigureAwait(false);
        await ExecuteSelectAsync(connection).ConfigureAwait(false);
        await ExecuteDropAsync(connection).ConfigureAwait(false);
    }

    private static async Task ExecuteCommandAsync(string commandString, SqlConnection connection)
    {
        try
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities. All queries are static strings.
            using var command = new SqlCommand(commandString, connection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities. All queries are static strings.
            using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            Console.WriteLine($"Async SQL query executed successfully: {commandString}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while executing async SQL query: {commandString}.\n{ex.Message}");
        }
    }

    private static async Task ExecuteCreateAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(CreateCommand, connection).ConfigureAwait(false);
    }

    private static async Task ExecuteInsertAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(InsertCommand, connection).ConfigureAwait(false);
    }

    private static async Task ExecuteSelectAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(SelectCommand, connection).ConfigureAwait(false);
    }

    private static async Task ExecuteDropAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(DropCommand, connection).ConfigureAwait(false);
    }

    private static string GetConnectionString(string databasePassword, string databasePort)
    {
        return $"Server=127.0.0.1,{databasePort};User=sa;Password={databasePassword};TrustServerCertificate=True;";
    }

    private static (string DatabasePassword, string Port) ParseArgs(string[] args)
    {
        if (args?.Length != 2)
        {
            throw new ArgumentException($"{nameof(TestApplication.SqlClient)}: requires two command-line arguments: <dbPassword> <dbPort>");
        }

        return (DatabasePassword: args[0], Port: args[1]);
    }
}
