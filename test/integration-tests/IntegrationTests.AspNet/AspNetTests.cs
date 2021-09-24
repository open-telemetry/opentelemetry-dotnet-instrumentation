using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.AspNet
{
    public class AspNetTests : TestHelper
    {
        public AspNetTests(ITestOutputHelper output)
            : base("AspNet", output)
        {
        }

        [Fact]
        [Trait("Category", "EndToEnd")]
        [Trait("WindowsOnly", "True")]
        public void SubmitsTraces()
        {
            int agentPort = TcpPortProvider.GetOpenPort();

            // using (var agent = new MockZipkinCollector(Output, agentPort))
        }
    }
}
