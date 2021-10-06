#if !NET452
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class ServiceBusMinimalRebusTest : SmokeTestBase
    {
        public ServiceBusMinimalRebusTest(ITestOutputHelper output)
            : base(output, "ServiceBus.Minimal.Rebus", maxTestRunSeconds: 90)
        {
        }

        [Fact]
        [Trait("Category", "Smoke")]
        public void NoExceptions()
        {
            CheckForSmoke(shouldDeserializeTraces: false);
        }
    }
}
#endif
