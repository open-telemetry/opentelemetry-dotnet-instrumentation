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

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class LogTests : TestHelper
{
    public LogTests(ITestOutputHelper output)
        : base("Logs", output)
    {
    }

#if NET6_0_OR_GREATER
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
    public void EnableLogsWithCLRAndHostingStartup()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Information from Test App.\" }");

        SetEnvironmentVariable("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", "OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertExpectations();
    }
#endif

#if NET462
    [Trait("Category", "EndToEnd")]
    [Fact(Skip = "Fails")]
    public void SubmitLogs()
    {
        var includeFormattedMessage = true;
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

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", includeFormattedMessage.ToString());
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "false");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");

        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertExpectations();

        /*
        var includeFormattedMessage = false;
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

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", includeFormattedMessage.ToString());
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_NETFX_REDIRECT_ENABLED", "false");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");

        int aspNetCorePort = TcpPortProvider.GetOpenPort();
        SetEnvironmentVariable("ASPNETCORE_URLS", $"http://127.0.0.1:{aspNetCorePort}/");
        EnableBytecodeInstrumentation();

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);
        try
        {
            await HealthzHelper.TestAsync($"http://localhost:{aspNetCorePort}/alive-check", Output);
            var url = $"http://localhost:{aspNetCorePort}/test";

            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Output.WriteLine($"[http] {response.StatusCode} {content}");

            collector.AssertExpectations();
        }
        catch (HttpRequestException ex)
        {
            Output.WriteLine($"[http] exception: {ex}");
        }
        finally
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                process.WaitForExit();
                Output.WriteLine("Exit Code: " + process.ExitCode);
            }

            Output.WriteResult(helper);
        }*/
    }
#endif
}
