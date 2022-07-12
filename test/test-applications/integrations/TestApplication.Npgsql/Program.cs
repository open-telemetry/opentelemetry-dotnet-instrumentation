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
using Npgsql;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"Command line: {string.Join(";", args)}");
        Console.WriteLine($"Profiler attached: {IsProfilerAttached()}");
        Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");

        var postgresPort = GetNpgsqlPort(args);

        var connString = $"Server=127.0.0.1;Port={postgresPort};User ID=postgres";

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        using (var cmd = new NpgsqlCommand($@"SELECT {postgresPort};", conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                Console.WriteLine(reader.GetInt32(0));
            }
        }
    }

    private static string GetNpgsqlPort(string[] args)
    {
        if (args.Length > 0)
        {
            return args[1];
        }

        return "5432";
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
