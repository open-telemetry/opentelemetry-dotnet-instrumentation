#if !NET452
using Datadog.Core.Tools;
using Datadog.Trace.TestHelpers;
using DockerComposeFixture;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    [Collection("stackexchangeredis")]
    public class StackExchangeRedisStackOverflowExceptionSmokeTest : SmokeTestBase, IClassFixture<DockerFixture>
    {
        public StackExchangeRedisStackOverflowExceptionSmokeTest(ITestOutputHelper output, DockerFixture dockerFixture)
            : base(output, "StackExchange.Redis.StackOverflowException", maxTestRunSeconds: 30)
        {
            dockerFixture.InitOnce(() => new DockerFixtureOptions
            {
                DockerComposeFiles = new[] { "stackexchangeredis-docker-compose.yml" }
            });
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void NoExceptions()
        {
            if (EnvironmentTools.IsWindows())
            {
                Output.WriteLine("Ignored for Windows");
                return;
            }

            CheckForSmoke(shouldDeserializeTraces: false);
        }
    }
}
#endif
