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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using OpenTelemetry.Proto.Common.V1;
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
    public async Task ApplicationIsNotExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe");

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
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INCLUDE_PROCESSES", $"dotnet,dotnet.exe");

        var spans = await RunTestApplicationAsync();

#if NETFRAMEWORK
        AssertNoSpansReceived(spans);
#else
        // FIXME: OTEL_DOTNET_AUTO_INCLUDE_PROCESSES does on .NET Core.
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
    public void SubmitMetrics()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        const int expectedMetricRequests = 1;

        using var collector = new MockMetricsCollector(Output);
        RunTestApplication(metricsAgentPort: collector.Port);
        var metricRequests = collector.WaitForMetrics(expectedMetricRequests, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            metricRequests.Count.Should().Be(expectedMetricRequests);

            var resourceMetrics = metricRequests.Single().ResourceMetrics.Single();

            var expectedServiceNameAttribute = new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = ServiceName } };
            resourceMetrics.Resource.Attributes.Should().ContainEquivalentOf(expectedServiceNameAttribute);

            var customClientScope = resourceMetrics.ScopeMetrics.Single(rm => rm.Scope.Name.Equals("MyCompany.MyProduct.MyLibrary", StringComparison.OrdinalIgnoreCase));
            var myFruitCounterMetric = customClientScope.Metrics.FirstOrDefault(m => m.Name.Equals("MyFruitCounter", StringComparison.OrdinalIgnoreCase));
            myFruitCounterMetric.Should().NotBeNull();
            myFruitCounterMetric.DataCase.Should().Be(OpenTelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Sum);
            myFruitCounterMetric.Sum.DataPoints.Count.Should().Be(1);

            var myFruitCounterAttributes = myFruitCounterMetric.Sum.DataPoints[0].Attributes;
            myFruitCounterAttributes.Count.Should().Be(1);
            myFruitCounterAttributes.Single(a => a.Key == "name").Value.StringValue.Should().Be("apple");
        }
    }

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
                waitTime: 30.Seconds(),
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

            var expectations = new List<WebServerSpanExpectation>();
            expectations.Add(new WebServerSpanExpectation(ServiceName, "1.0.0", "SayHello", "MyCompany.MyProduct.MyLibrary"));

#if NETFRAMEWORK
            expectations.Add(new WebServerSpanExpectation(ServiceName, "1.0.0.0", "HTTP GET", "OpenTelemetry.HttpWebRequest", httpMethod: "GET"));
#else
            expectations.Add(new WebServerSpanExpectation(ServiceName, "1.0.0.0", "HTTP GET", "OpenTelemetry.Instrumentation.Http", httpMethod: "GET"));
#endif

            SpanTestHelpers.AssertExpectationsMet(expectations, spans);
        }
    }

    private async Task<IImmutableList<IMockSpan>> RunTestApplicationAsync(bool enableStartupHook = true)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");

        using var agent = new MockZipkinCollector(Output);
        RunTestApplication(agent.Port, enableStartupHook: enableStartupHook);

        return await agent.WaitForSpansAsync(2, TimeSpan.FromSeconds(5));
    }
}
