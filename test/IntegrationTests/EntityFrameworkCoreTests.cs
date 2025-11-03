// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class EntityFrameworkCoreTests : TestHelper
{
    public EntityFrameworkCoreTests(ITestOutputHelper output)
        : base("EntityFrameworkCore", output)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.EntityFrameworkCore), MemberType = typeof(LibraryVersion))]
    public void SubmitTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.Instrumentation.EntityFrameworkCore");

        RunTestApplication(new TestSettings { PackageVersion = packageVersion });

        collector.AssertExpectations();
    }
}
#endif
