// <copyright file="AspNetTests.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Opentelemetry.Proto.Common.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AspNetTests : TestHelper
{
    private static readonly string TestApplicationDir = Path.Combine("test", "test-applications", "integrations", "aspnet");

    public AspNetTests(ITestOutputHelper output)
        : base(new EnvironmentHelper("AspNet", typeof(TestHelper), output, TestApplicationDir), output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task SubmitsTraces()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        var agentPort = TcpPortProvider.GetOpenPort();
        var webPort = TcpPortProvider.GetOpenPort();

        var testSettings = new TestSettings
        {
            TracesSettings = new TracesSettings { Port = agentPort }
        };

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var fwPort = FirewallHelper.OpenWinPort(agentPort, Output);
        using var agent = new MockZipkinCollector(Output, agentPort, host: "*");
        using var container = await StartContainerAsync(testSettings, webPort);

        var client = new HttpClient();

        var response = await client.GetAsync($"http://localhost:{webPort}");
        var content = await response.Content.ReadAsStringAsync();

        Output.WriteLine("Sample response:");
        Output.WriteLine(content);

        agent.SpanFilters.Add(x => x.Name != "healthz");

        var spans = agent.WaitForSpans(1);

        Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task SubmitsMetrics()
    {
        var collectorPort = TcpPortProvider.GetOpenPort();
        var webPort = TcpPortProvider.GetOpenPort();
        const int expectedMetricRequests = 1;

        var testSettings = new TestSettings
        {
            MetricsSettings = new MetricsSettings { Port = collectorPort },
            EnableStartupHook = true
        };

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var fwPort = FirewallHelper.OpenWinPort(collectorPort, Output);
        using var collector = new MockCollector(Output, collectorPort, host: "*");
        using var container = await StartContainerAsync(testSettings, webPort);

        var client = new HttpClient();

        var response = await client.GetAsync($"http://localhost:{webPort}");
        var content = await response.Content.ReadAsStringAsync();

        Output.WriteLine("Sample response:");
        Output.WriteLine(content);

        var metricRequests = collector.WaitForMetrics(expectedMetricRequests, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            metricRequests.Count.Should().BeGreaterThanOrEqualTo(expectedMetricRequests);
            var resourceMetrics = metricRequests.LastOrDefault().ResourceMetrics.Single();

            var expectedServiceNameAttribute = new KeyValue { Key = "service.name", Value = new AnyValue { StringValue = "TestApplication.AspNet" } };
            resourceMetrics.Resource.Attributes.Should().ContainEquivalentOf(expectedServiceNameAttribute);
            var aspnetScope = resourceMetrics.ScopeMetrics.Single(rm => rm.Scope.Name.Equals("OpenTelemetry.Instrumentation.AspNet", StringComparison.OrdinalIgnoreCase));

            var httpServerDurationMetric = aspnetScope.Metrics.FirstOrDefault(m => m.Name.Equals("http.server.duration", StringComparison.OrdinalIgnoreCase));
            httpServerDurationMetric.Should().NotBeNull();
        }
    }
}
#endif
