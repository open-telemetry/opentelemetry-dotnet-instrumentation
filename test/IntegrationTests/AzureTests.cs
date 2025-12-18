// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(AzureCollection.Name)]
public class AzureTests : TestHelper
{
    private readonly AzureFixture _azure;

    public AzureTests(ITestOutputHelper output, AzureFixture azure)
        : base("Azure", output)
    {
        _azure = azure;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Azure), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        collector.Expect("Azure.Core.Http");
        collector.Expect("Azure.Storage.Blobs.BlobContainerClient");

#if NET
        collector.Expect("System.Net.Http");
#elif NETFRAMEWORK
        // On .NET Framework the "OpenTelemetry.Instrumentation.Http.HttpWebRequest"
        // ends up being suppressed by the addition of headers via the Azure instrumentation
        // See https://github.com/open-telemetry/opentelemetry-dotnet/blob/f6a1c04e8115a828dd33269f639daf2924796bae/src/OpenTelemetry.Instrumentation.Http/Implementation/HttpWebRequestActivitySource.netfx.cs#L279-L284
#endif

        RunTestApplication(new()
        {
            Arguments = $"{_azure.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
