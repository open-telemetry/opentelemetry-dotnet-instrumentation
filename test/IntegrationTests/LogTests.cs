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

#if !NETFRAMEWORK

using System;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
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
    public async Task SubmitLogs(bool enableClrProfiler, bool includeFormattedMessage)
    {
        using var collector = await MockLogsCollector.Start(Output);
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
            collector.Expect(logRecord => Convert.ToString(logRecord).Contains("TestApplication.Logs.Controllers.TestController"));
        }

        if (enableClrProfiler)
        {
            SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
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
        using var collector = await MockLogsCollector.Start(Output);
        SetExporter(collector);
        collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Information from Test App.\" }");

        SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
