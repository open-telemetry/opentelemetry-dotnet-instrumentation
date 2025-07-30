// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests.FileBased;

public class LogsTests : TestHelper
{
    public LogsTests(ITestOutputHelper output)
        : base("Logs", output)
    {
    }

    [Fact]
    public void SubmitLogs()
    {
        using var collector = new MockLogsCollector(Output, 4318);
        SetExporter(collector);

        // When includeFormattedMessage is set to false
        // LogRecord is not parsed and body will not have data.
        // This is a default collector behavior.
        collector.Expect(logRecord =>
        {
            var logsAsString = Convert.ToString(logRecord);
            return logsAsString != null && logsAsString.Contains("TestApplication.Logs.Controllers.TestController");
        });

        SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");

        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif

