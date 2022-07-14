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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AspNetTests : TestHelper
{
    public AspNetTests(ITestOutputHelper output)
        : base("AspNet", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task SubmitsTraces()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var agent = new MockZipkinCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(agent.Port, Output);
        var testSettings = new TestSettings
        {
            TracesSettings = new TracesSettings { Port = agent.Port }
        };
        var webPort = TcpPortProvider.GetOpenPort();
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
    public async Task SubmitMetrics()
    {
        // Helps to reduce noice by enabling only AspNet metrics.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS", "AspNet");

        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        const int expectedMetricRequests = 1;

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockMetricsCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        var testSettings = new TestSettings
        {
            MetricsSettings = new MetricsSettings { Port = collector.Port },
        };
        var webPort = TcpPortProvider.GetOpenPort();
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
            var resourceMetrics = metricRequests.SelectMany(r => r.ResourceMetrics).Where(s => s.ScopeMetrics.Count > 0).FirstOrDefault();
            var aspnetMetrics = resourceMetrics.ScopeMetrics.Should().ContainSingle(x => x.Scope.Name == "OpenTelemetry.Instrumentation.AspNet").Which.Metrics;
            aspnetMetrics.Should().ContainSingle(x => x.Name == "http.server.duration");
        }
    }
}
#endif
