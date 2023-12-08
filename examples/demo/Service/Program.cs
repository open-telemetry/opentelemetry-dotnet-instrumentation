// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Data.SqlClient;

// .NET Diagnostics: create the span factory
using var activitySource = new ActivitySource("Examples.Service");

// .NET Diagnostics: create a metric
using var meter = new Meter("Examples.Service", "1.0");
var successCounter = meter.CreateCounter<long>("srv.successes.count", description: "Number of successful responses");

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
var connectionString = System.Environment.GetEnvironmentVariable("DB_CONNECTION");
app.MapGet("/", Handler);
app.Run();

async Task<string> Handler(ILogger<Program> logger)
{
    await ExecuteSql("SELECT 1");

    // .NET Diagnostics: create a manual span
    using (var activity = activitySource.StartActivity("SayHello"))
    {
        activity?.SetTag("foo", 1);
        activity?.SetTag("bar", "Hello, World!");
        activity?.SetTag("baz", new int[] { 1, 2, 3 });

        var waitTime = Random.Shared.NextDouble(); // max 1 seconds
        await Task.Delay(TimeSpan.FromSeconds(waitTime));

        activity?.SetStatus(ActivityStatusCode.Ok);

        // .NET Diagnostics: update the metric
        successCounter.Add(1);
    }

    // .NET ILogger: create a log
    logger.LogInformation("Success! Today is: {Date:MMMM dd, yyyy}", DateTimeOffset.UtcNow);

    return "Hello there";
}

async Task ExecuteSql(string sql)
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    using var command = new SqlCommand(sql, connection);
    using var reader = await command.ExecuteReaderAsync();
}
