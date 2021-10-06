#if !NET452
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class ServiceBusMinimalMassTransitTest : SmokeTestBase
    {
        public ServiceBusMinimalMassTransitTest(ITestOutputHelper output)
            : base(output, "ServiceBus.Minimal.MassTransit", maxTestRunSeconds: 60)
        {
            AssumeSuccessOnTimeout = true;
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
