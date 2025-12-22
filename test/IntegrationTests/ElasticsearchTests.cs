// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class ElasticsearchTests : TestHelper
{
    public ElasticsearchTests(ITestOutputHelper output)
        : base("Elasticsearch", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Elasticsearch), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        if (string.IsNullOrEmpty(packageVersion) || new Version(packageVersion).CompareTo(new Version(8, 10, 0)) >= 0)
        {
            collector.Expect("Elastic.Transport");
        }
        else
        {
            collector.Expect("Elastic.Clients.Elasticsearch.ElasticsearchClient");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }
}
