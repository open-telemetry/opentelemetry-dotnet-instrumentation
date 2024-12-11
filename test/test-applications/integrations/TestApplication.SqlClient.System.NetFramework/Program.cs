// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data.SqlClient;
using System.Diagnostics;
using TestApplication.Shared;

namespace TestApplication.SqlClient.System;

/// <summary>
/// This test application uses SqlConnection from System.Data (shipped with .NET Framework).
/// </summary>
public class Program
{
    private const string CreateCommand = "CREATE TABLE MY_TABLE ( Id int, Value1 varchar(255), Value2 varchar(255) )";
    private const string DropCommand = "DROP TABLE MY_TABLE";
    private const string InsertCommand = "INSERT INTO MY_TABLE VALUES ( 1, 'value1', 'value2' )";
    private const string SelectCommand = "SELECT * FROM MY_TABLE";

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        const string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Connect Timeout=30;TrustServerCertificate=True;";

        using (var connection = new SqlConnection(connectionString))
        {
            ExecuteCommands(connection);
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await ExecuteAsyncCommands(connection);
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
            Process.GetCurrentProcess().WaitForExit();
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
            using var command = new SqlCommand(commandString, connection);
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
        await connection.OpenAsync();
        await ExecuteCreateAsync(connection);
        await ExecuteInsertAsync(connection);
        await ExecuteSelectAsync(connection);
        await ExecuteDropAsync(connection);
    }

    private static async Task ExecuteCommandAsync(string commandString, SqlConnection connection)
    {
        try
        {
            using var command = new SqlCommand(commandString, connection);
            using var reader = await command.ExecuteReaderAsync();
            Console.WriteLine($"Async SQL query executed successfully: {commandString}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while executing async SQL query: {commandString}.\n{ex.Message}");
        }
    }

    private static async Task ExecuteCreateAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(CreateCommand, connection);
    }

    private static async Task ExecuteInsertAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(InsertCommand, connection);
    }

    private static async Task ExecuteSelectAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(SelectCommand, connection);
    }

    private static async Task ExecuteDropAsync(SqlConnection connection)
    {
        await ExecuteCommandAsync(DropCommand, connection);
    }
}
