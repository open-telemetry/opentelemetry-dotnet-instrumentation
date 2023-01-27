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
using System.Diagnostics.Metrics;
using System.Net.Http;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using TestApplication.NServiceBus;
using TestApplication.Shared;

namespace TestApplication.CustomSdk;

public static class Program
{
    private static readonly ActivitySource ActivitySource = new(
        "TestApplication.CustomSdk");

    private static readonly Meter Meter = new("TestApplication.CustomSdk");
    private static readonly Counter<long> Counter = Meter.CreateCounter<long>("RequestCounter");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // if auto-instrumentation is not injecting sdk
        // then it's client's code responsibility
        // to subscribe to all activity sources

        var tracerProvider = BuildTracerProvider();
        var meterProvider = BuildMeterProvider();

        AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
        {
            // autoinstrumentation disposes created instrumentations during AppDomain.CurrentDomain.ProcessExit
            // redis instrumentation flushes inside Dispose() which creates activities
            // delay providers disposal so that there are active listeners
            // when redis instrumentation attempts to create new activities
            tracerProvider?.Dispose();
            meterProvider?.Dispose();
        };

        await SendNServiceBusMessage();

        Counter.Add(1);

        using (var activity = ActivitySource.StartActivity("Manual"))
        {
            await PingRedis(args);

            using var client = new HttpClient();
            await client.GetStringAsync("https://www.bing.com");
        }
    }

    private static TracerProvider? BuildTracerProvider()
    {
        return Sdk
            .CreateTracerProviderBuilder()
            // bytecode instrumentation
            .AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis")
            // lazily-loaded instrumentation
            .AddSource("OpenTelemetry.Instrumentation.Http.HttpClient")
            .AddSource("System.Net.Http") // This works only System.Net.Http >= 7.0.0
            .AddLegacySource("System.Net.Http.HttpRequestOut")
            // custom activity source
            .AddSource("TestApplication.CustomSdk")
            .AddSource("NServiceBus.Core")
            .ConfigureResource(builder =>
                builder.AddAttributes(new[] { new KeyValuePair<string, object>("test_attr", "added_manually") }))
            .AddOtlpExporter()
            .Build();
    }

    private static MeterProvider? BuildMeterProvider()
    {
        return Sdk
            .CreateMeterProviderBuilder()
            // lazily-loaded metric instrumentation
            .AddMeter("OpenTelemetry.Instrumentation.*")
            // bytecode metric instrumentation
            .AddMeter("NServiceBus.Core")
            // custom metric
            .AddMeter("TestApplication.CustomSdk")
            .ConfigureResource(builder =>
                builder.AddAttributes(new[] { new KeyValuePair<string, object>("test_attr", "added_manually") }))
            .AddOtlpExporter()
            .Build();
    }

    private static async Task PingRedis(string[] args)
    {
        var redisPort = GetRedisPort(args);

        var connectionString = $@"127.0.0.1:{redisPort}";

        using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = connection.GetDatabase();

        db.Ping();
    }

    private static async Task SendNServiceBusMessage()
    {
        var endpointConfiguration = new EndpointConfiguration("TestApplication.NServiceBus");

        var learningTransport = new LearningTransport { StorageDirectory = Path.GetTempPath() };
        endpointConfiguration.UseTransport(learningTransport);

        using var cancellation = new CancellationTokenSource();
        var endpointInstance = await Endpoint.Start(endpointConfiguration, cancellation.Token);

        try
        {
            await endpointInstance.SendLocal(new TestMessage(), cancellation.Token);
        }
        finally
        {
            await endpointInstance.Stop(cancellation.Token);
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
