// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// This test won't work outside of windows as it need the server side which is .NET Framework only.
#if NET && _WINDOWS
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfDotNetTests : WcfTestsBase
{
    public WcfDotNetTests(ITestOutputHelper output)
        : base("Wcf.Client.DotNet", output)
    {
    }

    [Trait("Category", "EndToEnd")]
    [Theory]
    [MemberData(nameof(LibraryVersion.WCFCoreClient), MemberType = typeof(LibraryVersion))]
    public async Task SubmitTraces(string clientPackageVersion)
    {
        EnableBytecodeInstrumentation();
        await SubmitsTracesInternal(clientPackageVersion).ConfigureAwait(true);
    }
}

#endif
