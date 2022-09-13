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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class LogTests : TestHelper
{
    public LogTests(ITestOutputHelper output)
        : base("Logs", output)
    {
        SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs(bool parseStateValues, bool includeFormattedMessage)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_PARSE_STATE_VALUES", parseStateValues.ToString());
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", includeFormattedMessage.ToString());

        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        using var collector = new MockLogsCollector(Output);
        using var process = StartTestApplication(logsAgentPort: collector.Port, aspNetCorePort: aspNetCorePort, enableClrProfiler: !IsCoreClr());

        var maxMillisecondsToWait = 15_000;
        var intervalMilliseconds = 500;
        var intervals = maxMillisecondsToWait / intervalMilliseconds;
        var success = false;

        // Send a request to server.
        while (intervals-- > 0)
        {
            try
            {
                success = SubmitRequest(aspNetCorePort) == HttpStatusCode.OK;
            }
            catch
            {
                // ignore
            }

            if (success)
            {
                Output.WriteLine("Received response from server.");
                break;
            }

            Thread.Sleep(intervalMilliseconds);
        }

        try
        {
            var assert = () =>
            {
                success.Should().BeTrue();
                var logRequests = collector.WaitForLogs(1, TimeSpan.FromSeconds(5));
                var logs = logRequests.SelectMany(r => r.ResourceLogs).Where(s => s.ScopeLogs.Count > 0).FirstOrDefault();
                var logRecords = logs.ScopeLogs.FirstOrDefault().LogRecords;
                if (parseStateValues || includeFormattedMessage)
                {
                    logRecords.Should().ContainSingle(x => Convert.ToString(x.Body) == "{ \"stringValue\": \"Information from Test App.\" }");
                }
                else
                {
                    // When parseStateValues and includeFormattedMessage are set to false
                    // LogRecord is not parsed and body will not have data.
                    // This is a collector behavior.
                    logRecords.Should().ContainSingle(x => Convert.ToString(x).Contains("TestApplication.Logs.Controllers.TestController"));
                }
            };

            assert.Should().NotThrowAfter(
                waitTime: 30.Seconds(),
                pollInterval: 1.Seconds());
        }
        finally
        {
            process.Kill();
        }
    }

    private HttpStatusCode SubmitRequest(int aspNetCorePort)
    {
        try
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"http://localhost:{aspNetCorePort}/test"),
            };

            var response = client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            return response.StatusCode;
        }
        catch (Exception ex)
        {
            Output.WriteLine($"Exception: {ex}");
        }

        return HttpStatusCode.BadRequest;
    }
}
#endif
