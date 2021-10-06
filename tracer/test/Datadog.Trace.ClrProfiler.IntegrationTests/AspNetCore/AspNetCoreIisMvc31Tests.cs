#if NETCOREAPP3_1
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
using System.Net;
using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.AspNetCore
{
    [Collection("IisTests")]
    public class AspNetCoreIisMvc31TestsInProcess : AspNetCoreIisMvc31Tests
    {
        public AspNetCoreIisMvc31TestsInProcess(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: true, enableRouteTemplateResourceNames: false)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetCoreIisMvc31TestsInProcessWithFeatureFlag : AspNetCoreIisMvc31Tests
    {
        public AspNetCoreIisMvc31TestsInProcessWithFeatureFlag(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: true, enableRouteTemplateResourceNames: true)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetCoreIisMvc31TestsOutOfProcess : AspNetCoreIisMvc31Tests
    {
        public AspNetCoreIisMvc31TestsOutOfProcess(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: false, enableRouteTemplateResourceNames: false)
        {
        }
    }

    [Collection("IisTests")]
    public class AspNetCoreIisMvc31TestsOutOfProcessWithFeatureFlag : AspNetCoreIisMvc31Tests
    {
        public AspNetCoreIisMvc31TestsOutOfProcessWithFeatureFlag(IisFixture fixture, ITestOutputHelper output)
            : base(fixture, output, inProcess: false, enableRouteTemplateResourceNames: true)
        {
        }
    }

    public abstract class AspNetCoreIisMvc31Tests : AspNetCoreIisMvcTestBase
    {
        private readonly IisFixture _iisFixture;
        private readonly string _testName;

        protected AspNetCoreIisMvc31Tests(IisFixture fixture, ITestOutputHelper output, bool inProcess, bool enableRouteTemplateResourceNames)
            : base("AspNetCoreMvc31", fixture, output, inProcess, enableRouteTemplateResourceNames)
        {
            _testName = GetTestName(nameof(AspNetCoreIisMvc31Tests));
            _iisFixture = fixture;
            _iisFixture.TryStartIis(this, inProcess ? IisAppType.AspNetCoreInProcess : IisAppType.AspNetCoreOutOfProcess);
        }

        [Theory]
        [Trait("Category", "EndToEnd")]
        [Trait("Category", "LinuxUnsupported")]
        [Trait("RunOnWindows", "True")]
        [MemberData(nameof(Data))]
        public async Task MeetsAllAspNetCoreMvcExpectations(string path, HttpStatusCode statusCode)
        {
            // We actually sometimes expect 2, but waiting for 1 is good enough
            var spans = await GetWebServerSpans(path, _iisFixture.Agent, _iisFixture.HttpPort, statusCode, expectedSpanCount: 1);

            var sanitisedPath = VerifyHelper.SanitisePathsForVerify(path);

            var settings = VerifyHelper.GetSpanVerifierSettings(sanitisedPath, (int)statusCode);

            // Overriding the type name here as we have multiple test classes in the file
            // Ensures that we get nice file nesting in Solution Explorer
            await Verifier.Verify(spans, settings)
                          .UseMethodName("_")
                          .UseTypeName(_testName);
        }
    }
}
#endif
