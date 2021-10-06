using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class ServiceBusMinimalNServiceBusTest : SmokeTestBase
    {
        public ServiceBusMinimalNServiceBusTest(ITestOutputHelper output)
            : base(output, "ServiceBus.Minimal.NServiceBus", maxTestRunSeconds: 90)
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
