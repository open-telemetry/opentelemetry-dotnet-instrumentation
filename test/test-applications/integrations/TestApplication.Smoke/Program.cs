// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using TestApplication.Shared;

namespace TestApplication.Smoke;

internal sealed class Program
{
    public const string SourceName = "MyCompany.MyProduct.MyLibrary";

    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        EmitTracesAndLogs();
        EmitMetrics();

        // The "LONG_RUNNING" environment variable is used by tests that access/receive
        // data that takes time to be produced.
        var longRunning = Environment.GetEnvironmentVariable("LONG_RUNNING");
        if (longRunning == "true")
        {
            // In this case it is necessary to ensure that the test has a chance to read the
            // expected data, only by keeping the application alive for some time that can
            // be ensured. Anyway, tests that set "LONG_RUNNING" env var to true are expected
            // to kill the process directly.
            Console.WriteLine("LONG_RUNNING is true, waiting for process to be killed...");
            Process.GetCurrentProcess().WaitForExit();
        }
    }

    private static void EmitTracesAndLogs()
    {
        var myActivitySource = new ActivitySource(SourceName, "1.0.0");

        using var activity = myActivitySource.StartActivity("SayHello");
        activity?.SetTag("foo", 1);
        activity?.SetTag("bar", "Hello, World!");
        activity?.SetTag("baz", (int[])[1, 2, 3]);

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(1)
        };

        try
        {
            client.GetStringAsync(new Uri("http://httpstat.us/200")).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning);
        });
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Example log message");
    }

    private static void EmitMetrics()
    {
        var myMeter = new Meter(SourceName, "1.0.0");
        var myFruitCounter = myMeter.CreateCounter<int>("MyFruitCounter");

        myFruitCounter.Add(1, new KeyValuePair<string, object?>("name", "apple"));
    }
}
