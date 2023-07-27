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
using OpenTelemetry.Proto.Trace.V1;
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
    [MemberData(nameof(LibraryVersion.Azure), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect(
            "OpenTelemetry.Instrumentation.Http.HttpClient",
            IsBlobSpan);

        RunTestApplication(new()
        {
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private static bool IsBlobSpan(Span span)
    {
        return span.Name == "HTTP PUT" && span.Attributes.Any(att => att.Key == "http.url" && att.Value.ToString().StartsWith("{ \"stringValue\": \"http://127.0.0.1:10000/devstoreaccount1/test-container-"));
    }
}
