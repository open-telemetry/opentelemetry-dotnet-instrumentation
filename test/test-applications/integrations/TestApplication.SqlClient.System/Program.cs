// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Data.SqlClient;
using TestApplication.Shared;

namespace TestApplication.SqlClient.System;
#pragma warning disable CS0618 // Type or member is obsolete, System.Data.SqlClient classes are deprecated in 4.9.0+

/// <summary>
/// This test application uses SqlConnection from System.Data.SqlClient (NuGet package).
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

        (string databasePassword, string databasePort) = ParseArgs(args);
        var connectionString = GetConnectionString(databasePassword, databasePort);

        using (var connection = new SqlConnection(connectionString))
        {
            ExecuteCommands(connection);
        }

        using (var connection = new SqlConnection(connectionString))
        {
            await ExecuteAsyncCommands(connection);
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

    private static string GetConnectionString(string databasePassword, string databasePort)
    {
        return $"Server=127.0.0.1,{databasePort};User=sa;Password={databasePassword};TrustServerCertificate=True;";
    }

    private static (string DatabasePassword, string Port) ParseArgs(IReadOnlyList<string> args)
    {
        if (args?.Count != 2)
        {
            throw new ArgumentException($"{nameof(TestApplication.SqlClient)}: requires two command-line arguments: <dbPassword> <dbPort>");
        }

        return (DatabasePassword: args[0], Port: args[1]);
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
