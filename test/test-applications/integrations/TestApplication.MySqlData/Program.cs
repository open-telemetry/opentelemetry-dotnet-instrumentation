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
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using TestApplication.Shared;

namespace TestApplication.MySqlData;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var mySqlPort = GetMySqlPort(args);

        var connString = $@"Server=127.0.0.1;Port={mySqlPort};Uid=root";

        using var connection = new MySqlConnection(connString);
        await connection.OpenAsync();

        using var cmd = new MySqlCommand(@"SELECT 123;", connection);
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
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
