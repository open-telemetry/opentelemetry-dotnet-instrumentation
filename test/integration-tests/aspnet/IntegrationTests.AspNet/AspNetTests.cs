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

    [Fact]
    [Trait("Category", "EndToEnd")]
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
}
