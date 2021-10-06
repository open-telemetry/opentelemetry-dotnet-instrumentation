// The conditions looks weird, but it seems like _OR_GREATER is not supported yet in all environments
// We can trim all the additional conditions when this is fixed
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests
{
    public class AspNetCore5 : AspNetBase, IDisposable
    {
        public AspNetCore5(ITestOutputHelper outputHelper)
            : base("AspNetCore5", outputHelper, "/shutdown")
        {
        }

        // NOTE: by integrating the latest version of the WAF, blocking was disabled, as it does not support blocking yet
        [Theory]
        [InlineData(true, true, HttpStatusCode.OK)]
        [InlineData(true, false, HttpStatusCode.OK)]
        [InlineData(false, true, HttpStatusCode.OK)]
        [InlineData(false, false, HttpStatusCode.OK)]
        [Trait("RunOnWindows", "True")]
        [Trait("Category", "ArmUnsupported")]
        public async Task TestSecurity(bool enableSecurity, bool enableBlocking, HttpStatusCode expectedStatusCode)
        {
            using var agent = await RunOnSelfHosted(enableSecurity, enableBlocking);
            await TestBlockedRequestAsync(agent, enableSecurity, expectedStatusCode, 5, new Action<TestHelpers.MockTracerAgent.Span>[]
            {
             s => Assert.Equal("aspnet_core.request", s.Name),
             s  => Assert.Equal("Samples.AspNetCore5", s.Service),
             s  =>  Assert.Equal("web", s.Type)
            });
        }
    }
}
#endif
