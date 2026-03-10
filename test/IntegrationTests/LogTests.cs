// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Globalization;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class LogTests : TestHelper
{
    public LogTests(ITestOutputHelper output)
        : base("Logs", output)
    {
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs(bool enableClrProfiler, bool includeFormattedMessage)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        if (includeFormattedMessage)
        {
            collector.Expect(logRecord => Convert.ToString(logRecord.Body, CultureInfo.InvariantCulture) == "{ \"stringValue\": \"Information from Test App.\" }");
        }
        else
        {
            // When includeFormattedMessage is set to false
            // LogRecord is not parsed and body will not have data.
            // This is a default collector behavior.
            collector.Expect(logRecord =>
            {
                var logsAsString = Convert.ToString(logRecord, CultureInfo.InvariantCulture);
                return logsAsString != null && logsAsString.Contains("TestApplication.Logs.Controllers.TestController", StringComparison.Ordinal);
            });
        }

        if (enableClrProfiler)
        {
            EnableBytecodeInstrumentation();
        }
        else
        {
            SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
        }

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", includeFormattedMessage.ToString());
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    public void SubmitLogsFileBased()
    {
        using var collector = new MockLogsCollector(Output);
        SetFileBasedExporter(collector);
        EnableFileBasedConfigWithDefaultPath();

        // When includeFormattedMessage is set to false
        // LogRecord is not parsed and body will not have data.
        // This is a default collector behavior.
        collector.Expect(logRecord =>
        {
            var logsAsString = Convert.ToString(logRecord, CultureInfo.InvariantCulture);
            return logsAsString != null && logsAsString.Contains("TestApplication.Logs.Controllers.TestController", StringComparison.Ordinal);
        });

        SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");

        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    public async Task EnableLogsWithCLRAndHostingStartup()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        collector.ExpectCollected(ValidateSingleAppLogRecord, "App log record should be exported once.");

        SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        // wait for fixed amount of time for logs to be collected before asserting
        await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(true);

        collector.AssertCollected();
    }

    private static bool ValidateSingleAppLogRecord(IEnumerable<LogRecord> records)
    {
        return records.Count(lr => Convert.ToString(lr.Body, CultureInfo.InvariantCulture) == "{ \"stringValue\": \"Information from Test App.\" }") == 1;
    }
}
#endif
