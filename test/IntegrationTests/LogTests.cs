// <copyright file="LogTests.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER
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
            collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Information from Test App.\" }");
        }
        else
        {
            // When includeFormattedMessage is set to false
            // LogRecord is not parsed and body will not have data.
            // This is a default collector behavior.
            collector.Expect(logRecord =>
            {
                var logsAsString = Convert.ToString(logRecord);
                return logsAsString != null && logsAsString.Contains("TestApplication.Logs.Controllers.TestController");
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
        await Task.Delay(TimeSpan.FromSeconds(10));

        collector.AssertCollected();
    }

    private static bool ValidateSingleAppLogRecord(IEnumerable<LogRecord> records)
    {
        return records.Count(lr => Convert.ToString(lr.Body) == "{ \"stringValue\": \"Information from Test App.\" }") == 1;
    }
}
#endif
