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

using System.Diagnostics;
using System.Net.Http;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using TestApplication.Shared;

namespace TestApplication.CustomSdk;

public static class Program
{
    private static readonly ActivitySource ActivitySource = new(
        "TestApplication.CustomSdk");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // if auto-instrumentation is not injecting sdk
        // then it's client's code responsibility
        // to subscribe to all activity sources
        using var tracerProvider = Sdk
            .CreateTracerProviderBuilder()
            // bytecode instrumentation
            .AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis")
            // lazily-loaded instrumentation
            .AddSource("OpenTelemetry.Instrumentation.Http.HttpClient")
            .AddSource("System.Net.Http") // This works only System.Net.Http >= 7.0.0
            .AddLegacySource("System.Net.Http.HttpRequestOut")
            // custom activity source
            .AddSource("TestApplication.CustomSdk")
            .ConfigureResource(builder =>
                builder.AddAttributes(new[] { new KeyValuePair<string, object>("test_attr", "added_manually") }))
            .AddOtlpExporter()
            .AddConsoleExporter()
            .Build();

        var redisPort = GetRedisPort(args);

        var connectionString = $@"127.0.0.1:{redisPort}";

        using (var activity = ActivitySource.StartActivity("Manual"))
        {
            using (var connection = await ConnectionMultiplexer.ConnectAsync(connectionString))
            {
                var db = connection.GetDatabase();

                db.Ping();
            }

            using var client = new HttpClient();
            await client.GetStringAsync("https://www.bing.com");
        }

        Thread.Sleep(TimeSpan.FromSeconds(10));
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
