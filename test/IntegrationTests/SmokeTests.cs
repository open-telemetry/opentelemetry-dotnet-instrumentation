// <copyright file="SmokeTests.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class SmokeTests : TestHelper
{
    private const string ServiceName = "TestApplication.Smoke";

    public SmokeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "HttpClient");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        var spans = await RunTestApplicationAsync();

        AssertAllSpansReceived(spans);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task WhenStartupHookIsNotEnabled()
    {
        var spans = await RunTestApplicationAsync(enableStartupHook: false);

#if NETFRAMEWORK
        AssertAllSpansReceived(spans);
#else
        // on .NET Core it is required to set DOTNET_STARTUP_HOOKS
        AssertNoSpansReceived(spans);
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task WhenClrProfilerIsNotEnabled()
    {
        var spans = await RunTestApplicationAsync(enableClrProfiler: false);

#if NETFRAMEWORK
        // on .NET Framework it is required to set the CLR .NET Profiler
        AssertNoSpansReceived(spans);
#else
        AssertAllSpansReceived(spans);
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task ApplicationIsNotExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "dotnet,dotnet.exe");

        var spans = await RunTestApplicationAsync();

        AssertAllSpansReceived(spans);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task ApplicationIsExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe,{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");

        var spans = await RunTestApplicationAsync();

        AssertNoSpansReceived(spans);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task ApplicationIsNotIncluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PROCESSES", "dotnet,dotnet.exe");

        var spans = await RunTestApplicationAsync();

#if NETFRAMEWORK
        AssertNoSpansReceived(spans);
#else
        // FIXME: OTEL_DOTNET_AUTO_INCLUDE_PROCESSES does not work on .NET Core.
        // https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/895
        AssertAllSpansReceived(spans);
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task ApplicationIsIncluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PROCESSES", $"{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");

        var spans = await RunTestApplicationAsync();

        AssertAllSpansReceived(spans);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitMetrics()
    {
        using var collector = await MockMetricsCollector.Start(Output);
        collector.Expect("MyCompany.MyProduct.MyLibrary", metric => metric.Name == "MyFruitCounter");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(metricsAgentPort: collector.Port);

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task TracesResource()
    {
        using var collector = await MockSpansCollector.Start(Output);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(otlpTraceCollectorPort: collector.Port);

        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task MetricsResource()
    {
        using var collector = await MockMetricsCollector.Start(Output);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(metricsAgentPort: collector.Port);

        collector.ResourceExpector.AssertExpectations();
    }

#if !NETFRAMEWORK // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task LogsResource()
    {
        using var collector = await MockLogsCollector.Start(Output);
        collector.ResourceExpector.Expect("service.name", ServiceName);
        collector.ResourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        collector.ResourceExpector.Expect("telemetry.sdk.language", "dotnet");
        collector.ResourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
        collector.ResourceExpector.Expect("telemetry.auto.version", OpenTelemetry.AutoInstrumentation.Constants.Tracer.Version);

        RunTestApplication(logsAgentPort: collector.Port);

        collector.ResourceExpector.AssertExpectations();
    }
#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OtlpTracesExporter()
    {
        using var collector = await MockSpansCollector.Start(Output);
        collector.Expect("MyCompany.MyProduct.MyLibrary", span => span.Name == "SayHello");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(otlpTraceCollectorPort: collector.Port);

        collector.AssertExpectations();
    }

#if NETFRAMEWORK // The test is flaky on Linux and macOS, becasue of https://github.com/dotnet/runtime/issues/28658#issuecomment-462062760
    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task PrometheusExporter()
    {
        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "prometheus");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        const string defaultPrometheusMetricsEndpoint = "http://localhost:9464/metrics";

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);

        try
        {
            var assert = async () =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = 5.Seconds()
                };
                var response = await httpClient.GetAsync(defaultPrometheusMetricsEndpoint);
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();
                Output.WriteLine("Raw metrics from Prometheus:");
                Output.WriteLine(content);
                content.Should().Contain("TYPE ", "should export any metric");
            };
            await assert.Should().NotThrowAfterAsync(
                waitTime: 1.Minutes(),
                pollInterval: 1.Seconds());
        }
        finally
        {
            if (!helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();
            }

            Output.WriteLine("ProcessId: " + helper.Process.Id);
            Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
            Output.WriteResult(helper);
        }
    }
#endif

#if !NETFRAMEWORK // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitLogs()
    {
        using var collector = await MockLogsCollector.Start(Output);
        collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Example log message\" }");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        RunTestApplication(logsAgentPort: collector.Port, enableClrProfiler: true);

        collector.AssertExpectations();
    }
#endif

    private static void AssertNoSpansReceived(IImmutableList<IMockSpan> spans)
    {
        Assert.True(spans.Count == 0, $"Expecting no spans, received {spans.Count}");
    }

    private static void AssertAllSpansReceived(IImmutableList<IMockSpan> spans)
    {
        var expectedSpanCount = 2;
        Assert.True(spans.Count() == expectedSpanCount, $"Expecting {expectedSpanCount} spans, received {spans.Count()}");
        if (expectedSpanCount > 0)
        {
            Assert.Single(spans.Select(s => s.Service).Distinct());

            var expectations = new List<SpanExpectation>();
            expectations.Add(new SpanExpectation(ServiceName, "1.0.0", "SayHello", "MyCompany.MyProduct.MyLibrary", ActivityKind.Internal));

#if NETFRAMEWORK
            expectations.Add(new SpanExpectation(ServiceName, "1.0.0.0", "HTTP GET", "OpenTelemetry.HttpWebRequest", ActivityKind.Client));
#else
            expectations.Add(new SpanExpectation(ServiceName, "1.0.0.0", "HTTP GET", "OpenTelemetry.Instrumentation.Http", ActivityKind.Client));
#endif

            SpanTestHelpers.AssertExpectationsMet(expectations, spans);
        }
    }

    private async Task<IImmutableList<IMockSpan>> RunTestApplicationAsync(bool enableStartupHook = true, bool enableClrProfiler = true)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");

        using var agent = await LegacyMockZipkinCollector.Start(Output);
        RunTestApplication(agent.Port, enableStartupHook: enableStartupHook, enableClrProfiler: enableClrProfiler);

        // The process emitting the spans already finished by this time, there is no need to wait more,
        // just get the spans received so far.
        return await agent.WaitForSpansAsync(count: 2, timeout: TimeSpan.Zero);
    }
}
