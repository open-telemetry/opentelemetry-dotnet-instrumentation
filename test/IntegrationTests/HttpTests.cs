// <copyright file="HttpTests.cs" company="OpenTelemetry Authors">
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
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Opentelemetry.Proto.Common.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class HttpTests : TestHelper
{
    private const string ServiceName = "TestApplication.Http";

    public HttpTests(ITestOutputHelper output)
        : base("Http", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ENABLED_INSTRUMENTATIONS", "HttpClient,AspNet");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS", "HttpClient,AspNet");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitTraces()
    {
        var agentPort = TcpPortProvider.GetOpenPort();
        using var agent = new MockZipkinCollector(Output, agentPort);

        const int expectedSpanCount = 3;

        RunTestApplication(agent.Port);
        var spans = agent.WaitForSpans(expectedSpanCount, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            spans.Count.Should().Be(expectedSpanCount);

            // ASP.NET Core auto-instrumentation is generating spans
            var httpClientSpan = spans.FirstOrDefault(span => span.Name.Equals("HTTP GET"));
            var httpServerSpan = spans.FirstOrDefault(span => span.Name.Equals("/test"));
            var manualSpan = spans.FirstOrDefault(span => span.Name.Equals("manual span"));

            httpClientSpan.Should().NotBeNull();
            httpServerSpan.Should().NotBeNull();
            manualSpan.Should().NotBeNull();

            // checking trace hierarchy
            httpClientSpan.ParentId.HasValue.Should().BeFalse();
            httpServerSpan.ParentId.Should().Be(httpClientSpan.SpanId);
            manualSpan.ParentId.Should().Be(httpServerSpan.SpanId);

            httpClientSpan.Service.Should().Be(ServiceName);
            httpServerSpan.Service.Should().Be(ServiceName);
            manualSpan.Service.Should().Be(ServiceName);

            var httpClientTags = httpClientSpan.Tags;
            var httpServerTags = httpServerSpan.Tags;

            httpClientTags.Count.Should().Be(8);
            httpClientTags["http.method"].Should().Be("GET");
            httpClientTags["http.host"].Should().Be(httpServerTags["http.host"]);
            httpClientTags["http.url"].Should().Be(httpServerTags["http.url"]);
            httpClientTags["http.status_code"].Should().Be("200");
            httpClientTags["peer.service"].Should().Be(httpServerTags["http.host"]);
            httpClientTags["span.kind"].Should().Be("client");
            httpServerTags["span.kind"].Should().Be("server");
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        var collectorPort = TcpPortProvider.GetOpenPort();
        using var collector = new MockCollector(Output, collectorPort);

        const int expectedMetricRequests = 1;

        RunTestApplication(metricsAgentPort: collectorPort);
        var metricRequests = collector.WaitForMetrics(expectedMetricRequests, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            metricRequests.Count.Should().Be(expectedMetricRequests);

            var resourceMetrics = metricRequests.Single().ResourceMetrics.Single();

            var expectedServiceNameAttribute = new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = ServiceName } };
            resourceMetrics.Resource.Attributes.Should().ContainEquivalentOf(expectedServiceNameAttribute);

            var httpclientScope = resourceMetrics.ScopeMetrics.Single(rm => rm.Scope.Name.Equals("OpenTelemetry.Instrumentation.Http", StringComparison.OrdinalIgnoreCase));
            var aspnetcoreScope = resourceMetrics.ScopeMetrics.Single(rm => rm.Scope.Name.Equals("OpenTelemetry.Instrumentation.AspNetCore", StringComparison.OrdinalIgnoreCase));

            var httpClientDurationMetric = httpclientScope.Metrics.FirstOrDefault(m => m.Name.Equals("http.client.duration", StringComparison.OrdinalIgnoreCase));
            var httpServerDurationMetric = aspnetcoreScope.Metrics.FirstOrDefault(m => m.Name.Equals("http.server.duration", StringComparison.OrdinalIgnoreCase));

            httpClientDurationMetric.Should().NotBeNull();
            httpServerDurationMetric.Should().NotBeNull();

            httpClientDurationMetric.DataCase.Should().Be(Opentelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Histogram);
            httpServerDurationMetric.DataCase.Should().Be(Opentelemetry.Proto.Metrics.V1.Metric.DataOneofCase.Histogram);

            var httpClientDurationAttributes = httpClientDurationMetric.Histogram.DataPoints.Single().Attributes;
            var httpServerDurationAttributes = httpServerDurationMetric.Histogram.DataPoints.Single().Attributes;

            httpClientDurationAttributes.Count.Should().Be(4);
            httpClientDurationAttributes.Single(a => a.Key == "http.method").Value.StringValue.Should().Be("GET");
            httpClientDurationAttributes.Single(a => a.Key == "http.scheme").Value.StringValue.Should().Be("http");
            httpClientDurationAttributes.Single(a => a.Key == "http.flavor").Value.StringValue.Should().Be("1.1");
            httpClientDurationAttributes.Single(a => a.Key == "http.status_code").Value.IntValue.Should().Be(200);

            httpServerDurationAttributes.Count.Should().Be(5);
            httpServerDurationAttributes.Single(a => a.Key == "http.method").Value.StringValue.Should().Be("GET");
            httpClientDurationAttributes.Single(a => a.Key == "http.scheme").Value.StringValue.Should().Be("http");
            httpClientDurationAttributes.Single(a => a.Key == "http.flavor").Value.StringValue.Should().Be("1.1");
            httpServerDurationAttributes.Single(a => a.Key == "http.host").Value.StringValue.Should().StartWith("localhost");
            httpClientDurationAttributes.Single(a => a.Key == "http.status_code").Value.IntValue.Should().Be(200);
        }
    }
}
#endif
