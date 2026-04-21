// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Http;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using TestApplication.NServiceBus;
using TestApplication.Shared;

namespace TestApplication.CustomSdk;

internal static class Program
{
    private static readonly ActivitySource ActivitySource = new(
        "TestApplication.CustomSdk");

    private static readonly Meter Meter = new("TestApplication.CustomSdk");
    private static readonly Counter<long> Counter = Meter.CreateCounter<long>("RequestCounter");

    public static async Task Main(string[] args)
    {
        if (args.Length != 4)
        {
            throw new InvalidOperationException("Missing arguments. Provide redis port with --redis-port <redis-port> and test server port with --test-server-port <test-server-port>.");
        }

        ConsoleHelper.WriteSplashScreen(args);

        // When export of NServiceBus metrics is tested, which are updated on receive side,
        // test has to be marked as long running, in order to avoid random failures
        var longRunning = Environment.GetEnvironmentVariable("LONG_RUNNING") == "true";

        // if automatic instrumentation is not injecting sdk
        // then it's client's code responsibility
        // to subscribe to all activity sources

        var tracerProvider = BuildTracerProvider();
        var meterProvider = BuildMeterProvider();

        // When export of traces is tested, test is not marked as long running,
        // to avoid unnecessary delay due to default Redis instrumentation flush interval;
        // in this scenario it is required to add tracer/meter provider disposals on process exit
        if (!longRunning)
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                // automatic instrumentation disposes created instrumentations during AppDomain.CurrentDomain.ProcessExit
                // redis instrumentation flushes inside Dispose() which creates activities
                // delay providers disposal so that there are active listeners
                // when redis instrumentation attempts to create new activities
                tracerProvider?.Dispose();
                meterProvider?.Dispose();
            };
        }

        var endpointConfiguration = new EndpointConfiguration("TestApplication.NServiceBus");
        endpointConfiguration.UseSerialization<XmlSerializer>();

        var learningTransport = new LearningTransport { StorageDirectory = Path.GetTempPath() };
        endpointConfiguration.UseTransport(learningTransport);

        using var cancellation = new CancellationTokenSource();
        var endpointInstance = await Endpoint.Start(endpointConfiguration, cancellation.Token).ConfigureAwait(false);

        try
        {
            await endpointInstance.SendLocal(new TestMessage(), cancellation.Token).ConfigureAwait(false);

            Counter.Add(1);

            using (var activity = ActivitySource.StartActivity("Manual"))
            {
                await PingRedis(args).ConfigureAwait(false);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var port = int.Parse(args[3], CultureInfo.InvariantCulture);
                await client.GetStringAsync(new Uri($"http://localhost:{port}/test"), cancellation.Token).ConfigureAwait(false);
            }

            // The "LONG_RUNNING" environment variable is used by tests that access/receive
            // data that takes time to be produced.
            if (longRunning)
            {
                // In this case it is necessary to ensure that the test has a chance to read the
                // expected data, only by keeping the application alive for some time that can
                // be ensured. Anyway, tests that set "LONG_RUNNING" env var to true are expected
                // to kill the process directly.
                Console.WriteLine("LONG_RUNNING is true, waiting for process to be killed...");
                await Process.GetCurrentProcess().WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
            }
        }
        finally
        {
            await endpointInstance.Stop(cancellation.Token).ConfigureAwait(false);
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
                builder.AddAttributes([new KeyValuePair<string, object>("test_attr", "added_manually")]))
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
#if NET
            .AddMeter("NServiceBus.Core.Pipeline.Incoming")
#else
            .AddMeter("NServiceBus.Core")
#endif
            // custom metric
            .AddMeter("TestApplication.CustomSdk")
            .ConfigureResource(builder =>
                builder.AddAttributes([new KeyValuePair<string, object>("test_attr", "added_manually")]))
            .AddOtlpExporter()
            .Build();
    }

    private static async Task PingRedis(string[] args)
    {
        var redisPort = int.Parse(GetRedisPort(args), CultureInfo.InvariantCulture);

        var connectionString = $"127.0.0.1:{redisPort}";

        using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
        var db = connection.GetDatabase();

        await db.PingAsync().ConfigureAwait(false);
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
