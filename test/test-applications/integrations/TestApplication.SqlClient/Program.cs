// <copyright file="Program.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TestApplication.Shared;

namespace TestApplication.SqlClient
{
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
            return $"Server=localhost,{databasePort};User=sa;Password={databasePassword};TrustServerCertificate=True;";
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
}
