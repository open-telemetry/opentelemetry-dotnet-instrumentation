// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfDotNetTests : WcfTestsBase
{
    public WcfDotNetTests(ITestOutputHelper output)
        : base("Wcf.Client.DotNet", output)
    {
    }

    public static TheoryData<string, Func<ITestOutputHelper, WcfServerTestHelperBase>> TestData()
    {
        var theoryData = new TheoryData<string, Func<ITestOutputHelper, WcfServerTestHelperBase>>();

        foreach (var version in LibraryVersion.WCFCoreClient)
        {
#if _WINDOWS
            // This test won't work outside of windows as it need the server side which is .NET Framework only.
            theoryData.Add(version, output => new WcfServerTestHelper(output));
#endif
            foreach (var wcfCoreServerVersion in LibraryVersion.WCFCoreServer)
            {
                theoryData.Add(version, output => new WcfCoreServerTestHelper(output, wcfCoreServerVersion));
            }
        }

        return theoryData;
    }

    [Trait("Category", "EndToEnd")]
    [Theory]
    [MemberData(nameof(TestData))]
    public async Task SubmitTraces(string clientPackageVersion, Func<ITestOutputHelper, WcfServerTestHelperBase> wcfServerTestHelperFactory)
    {
        Assert.NotNull(wcfServerTestHelperFactory);
        EnableBytecodeInstrumentation();
        await SubmitsTracesInternal(clientPackageVersion, wcfServerTestHelperFactory(Output)).ConfigureAwait(true);
    }
}

#endif
