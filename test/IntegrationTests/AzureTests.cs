// <copyright file="AzureTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("Azure.Core.Http");
        collector.Expect("Azure.Storage.Blobs.BlobContainerClient");

#if NET7_0_OR_GREATER
        collector.Expect("System.Net.Http");
#elif NET6_0_OR_GREATER
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpClient");
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
