using System.IO;
using System.Linq;
using System.Net.Http;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.AspNet;

public class AspNetTests : TestHelper
{
    private static readonly string SampleDir = Path.Combine("test", "test-applications", "integrations", "aspnet");

    public AspNetTests(ITestOutputHelper output)
        : base(new EnvironmentHelper("AspNet", typeof(TestHelper), output, SampleDir), output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("WindowsOnly", "True")]
    public void SubmitsTraces()
    {
        var agentPort = TcpPortProvider.GetOpenPort();
        var webPort = TcpPortProvider.GetOpenPort();

        using (var fwPort = FirewallHelper.OpenWinPort(agentPort, Output))
        using (var agent = new MockZipkinCollector(Output, agentPort))
        using (var container = StartContainer(agentPort, webPort))
        {
            var client = new HttpClient();

            var response = client.GetAsync($"http://localhost:{webPort}").Result;
            var content = response.Content.ReadAsStringAsync().Result;

            Output.WriteLine("Sample response:");
            Output.WriteLine(content);

            var spans = agent.WaitForSpans(1);
            var webSpansCount = spans.Count(x => x.Name != "health-check");

            Assert.True(webSpansCount >= 1, $"Expecting at least 1 span, only received {spans.Count}");
        }
    }
}
