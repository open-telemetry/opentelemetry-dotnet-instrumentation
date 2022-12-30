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

// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Data.SqlClient;

// .NET Diagnostics: create the span factory
using var activitySource = new ActivitySource("MyCompany.Service");

// .NET Diagnostics: create a metric
using var meter = new Meter("MyCompany.Service", "1.0");
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
