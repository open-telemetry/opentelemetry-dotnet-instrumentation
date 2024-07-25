// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
