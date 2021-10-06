using Datadog.Trace.ClrProfiler;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Datadog.Trace.ClrProfiler.IntegrationTests.CustomTestFramework", "Datadog.Trace.ClrProfiler.IntegrationTests")]

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class CustomTestFramework : TestHelpers.CustomTestFramework
    {
        public CustomTestFramework(IMessageSink messageSink)
            : base(messageSink, typeof(Instrumentation))
        {
        }
    }
}
