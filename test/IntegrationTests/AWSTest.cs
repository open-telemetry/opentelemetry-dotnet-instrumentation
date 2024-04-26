// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(AWSCollection.Name)]
public class AWSTest : TestHelper
{
    private readonly AWSFixture _aws;

    public AWSTest(ITestOutputHelper output, AWSFixture awsFixture)
        : base("AWS", output)
    {
        _aws = awsFixture;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.AWS), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("Amazon.AWS.AWSClientInstrumentation");

        RunTestApplication(new()
        {
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
