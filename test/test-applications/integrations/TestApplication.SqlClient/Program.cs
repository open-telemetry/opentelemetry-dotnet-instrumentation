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
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace TestApplication.SqlClient
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine($"Command line: {string.Join(";", args)}");
            Console.WriteLine($"Profiler attached: {IsProfilerAttached()}");
            Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");

            var connectionString = GetConnectionString();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await ExecuteCreate(connection);
        }

        private static async Task ExecuteCreate(SqlConnection connection)
        {
            await using var command = new SqlCommand("CREATE TABLE MY_TABLE ( Id int, Value1 varchar(255), Value2 varchar(255) )", connection);
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                await using var stream = await reader.GetFieldValueAsync<Stream>(1);
            }
        }

        private static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")
                   ?? "Server=localhost;User=sa;Password=@someThingComplicated1234;Trusted_Connection=false;";
        }

        private static bool? IsProfilerAttached()
        {
            var instrumentationType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation", false);

            if (instrumentationType == null)
            {
                return null;
            }

            var property = instrumentationType.GetProperty("ProfilerAttached");
            var isAttached = property?.GetValue(null) as bool?;

            return isAttached ?? false;
        }
    }
}
