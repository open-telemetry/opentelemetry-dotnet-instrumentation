// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class QuartzTests : TestHelper
{
    public QuartzTests(ITestOutputHelper output)
        : base("Quartz", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Quartz), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        collector.Expect("OpenTelemetry.Instrumentation.Quartz");

        RunTestApplication(new TestSettings
        {
#if NET462
            Framework = "net472",
#endif
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
