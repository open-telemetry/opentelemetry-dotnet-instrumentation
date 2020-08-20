using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class AssemblyLoadFileNotFoundExceptionSmokeTest : SmokeTestBase
    {
        public AssemblyLoadFileNotFoundExceptionSmokeTest(ITestOutputHelper output)
            : base(output, "AssemblyLoad.FileNotFoundException")
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
