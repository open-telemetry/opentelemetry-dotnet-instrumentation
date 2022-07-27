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

using System.Threading.Tasks;
using StackExchange.Redis;
using TestApplication.Shared;

namespace TestApplication.StackExchangeRedis;

public static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var redisPort = GetRedisPort(args);

        var connectionString = $@"127.0.0.1:{redisPort}";

        using (var connection = await ConnectionMultiplexer.ConnectAsync(connectionString))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        using (var connection = await ConnectionMultiplexer.ConnectAsync(ConfigurationOptions.Parse(connectionString)))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        using (var connection = ConnectionMultiplexer.Connect(connectionString))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        using (var connection = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(connectionString)))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        // SentinelConnect and SentinelConnectAsync introduced in 2.1.50
        using (var connection = ConnectionMultiplexer.SentinelConnect(connectionString))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        using (var connection = ConnectionMultiplexer.SentinelConnect(ConfigurationOptions.Parse(connectionString)))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        using (var connection = await ConnectionMultiplexer.SentinelConnectAsync(connectionString))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }

        using (var connection = await ConnectionMultiplexer.SentinelConnectAsync(ConfigurationOptions.Parse(connectionString)))
        {
            var db = connection.GetDatabase();

            db.Ping();
        }
    }

    private static string GetRedisPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "6379";
    }
}
