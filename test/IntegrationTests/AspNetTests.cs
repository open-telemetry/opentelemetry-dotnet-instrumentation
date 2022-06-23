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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.AspNet;

public class AspNetTests : TestHelper
{
    private static readonly string TestApplicationDir = Path.Combine("test", "test-applications", "integrations", "aspnet");

    public AspNetTests(ITestOutputHelper output)
        : base(new EnvironmentHelper("AspNet", typeof(TestHelper), output, TestApplicationDir), output)
    {
    }

    [FactRequiringWindowsAdministrator]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task SubmitsTraces()
    {
        var agentPort = TcpPortProvider.GetOpenPort();
        var webPort = TcpPortProvider.GetOpenPort();

        using (var fwPort = FirewallHelper.OpenWinPort(agentPort, Output))
        using (var agent = new MockZipkinCollector(Output, agentPort))
        using (var container = await StartContainerAsync(agentPort, webPort))
        {
            var client = new HttpClient();

            var response = await client.GetAsync($"http://localhost:{webPort}");
            var content = await response.Content.ReadAsStringAsync();

            Output.WriteLine("Sample response:");
            Output.WriteLine(content);

            agent.SpanFilters.Add(x => x.Name != "healthz");

            var spans = agent.WaitForSpans(1);

            Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");
        }
    }

    public sealed class FactRequiringWindowsAdministratorAttribute : FactAttribute
    {
        public FactRequiringWindowsAdministratorAttribute()
        {
            if (!EnvironmentTools.IsWindowsAdministrator())
            {
                Skip = "This test requires Windows Administrator privileges";
            }
        }
    }
}
#endif
