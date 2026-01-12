// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Globalization;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WorkerTests : TestHelper
{
    public WorkerTests(ITestOutputHelper output)
        : base("Worker", output)
    {
    }

    [Fact]
    public async Task SubmitsLogsWithoutDuplicates()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(ValidateSingleAppLogRecord, "App log record should be exported once.");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");

        EnableBytecodeInstrumentation();

        RunTestApplication();

        // wait for fixed amount of time for logs to be collected before asserting
        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(true);

        collector.AssertCollected();
    }

    private static bool ValidateSingleAppLogRecord(IEnumerable<LogRecord> records)
    {
        return records.Count(lr => Convert.ToString(lr.Body, CultureInfo.InvariantCulture) == "{ \"stringValue\": \"Worker running.\" }") == 1;
    }
}

#endif
