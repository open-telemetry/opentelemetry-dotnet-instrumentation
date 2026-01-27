// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using StackExchange.Redis;
using TestApplication.Shared;

namespace TestApplication.StackExchangeRedis;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var redisPort = GetRedisPort(args);

        var connectionString = $@"127.0.0.1:{redisPort}";

        using (var connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false))
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

        using (var connection = await ConnectionMultiplexer.ConnectAsync(ConfigurationOptions.Parse(connectionString)).ConfigureAwait(false))
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

#pragma warning disable CA1849 // Call async methods when in an async method
        using (var connection = ConnectionMultiplexer.Connect(connectionString))
#pragma warning restore CA1849 // Call async methods when in an async method
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

#pragma warning disable CA1849 // Call async methods when in an async method
        using (var connection = ConnectionMultiplexer.Connect(ConfigurationOptions.Parse(connectionString)))
#pragma warning restore CA1849 // Call async methods when in an async method
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

#pragma warning disable CA1849 // Call async methods when in an async method
        using (var connection = ConnectionMultiplexer.SentinelConnect(connectionString))
#pragma warning restore CA1849 // Call async methods when in an async method
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

#pragma warning disable CA1849 // Call async methods when in an async method
        using (var connection = ConnectionMultiplexer.SentinelConnect(ConfigurationOptions.Parse(connectionString)))
#pragma warning restore CA1849 // Call async methods when in an async method
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

        using (var connection = await ConnectionMultiplexer.SentinelConnectAsync(connectionString).ConfigureAwait(false))
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
        }

        using (var connection = await ConnectionMultiplexer.SentinelConnectAsync(ConfigurationOptions.Parse(connectionString)).ConfigureAwait(false))
        {
            var db = connection.GetDatabase();

            await db.PingAsync().ConfigureAwait(false);
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
