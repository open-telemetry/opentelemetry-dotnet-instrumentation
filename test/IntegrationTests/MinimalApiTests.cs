// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class MinimalApiTests : TestHelper
{
    public MinimalApiTests(ITestOutputHelper output)
        : base("MinimalApi", output)
    {
    }

    [SkippableTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsLogsWithoutDuplicates(bool enableByteCodeInstrumentation, bool enableHostingStartupAssembly)
    {
        using var collector = await MockLogsCollector.InitializeAsync(Output);
        SetExporter(collector);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");

        // Bind to a random port.
        SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0");

        if (enableByteCodeInstrumentation)
        {
            EnableBytecodeInstrumentation();
            collector.ExpectCollected(ValidateCombinedLogsExport, "All log records should be exported once.");
        }

        if (enableHostingStartupAssembly)
        {
            SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
            if (!enableByteCodeInstrumentation)
            {
                // otherwise expectation was already set
                collector.ExpectCollected(ValidateSingleAppLogExport, "Application log records should be exported once.");
            }
        }

        RunTestApplication();

        // wait for fixed amount of time for logs to be collected before asserting
        await Task.Delay(TimeSpan.FromSeconds(10));

        collector.AssertCollected();
    }

    private static bool ValidateCombinedLogsExport(IEnumerable<LogRecord> records)
    {
        return ValidateSingleAppLogExport(records) && ValidateSingleBeforeHostLogRecord(records);
    }

    private static bool ValidateSingleBeforeHostLogRecord(IEnumerable<LogRecord> records)
    {
        var beforeHostLogCount = records.Count(lr => Convert.ToString(lr.Body) == "{ \"stringValue\": \"Logged before host is built.\" }");
        return beforeHostLogCount == 1;
    }

    private static bool ValidateSingleAppLogExport(IEnumerable<LogRecord> records)
    {
        var appLogCount = records.Count(lr => Convert.ToString(lr.Body) == "{ \"stringValue\": \"Request received.\" }");
        return appLogCount == 1;
    }
}
#endif
