// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;

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
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect(Quartz4Plus(packageVersion) ? "Quartz" : "OpenTelemetry.Instrumentation.Quartz");

        RunTestApplication(new TestSettings
        {
#if NET462
            Framework = "net472",
#endif
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

#if NET10_0_OR_GREATER
    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Quartz), MemberType = typeof(LibraryVersion))]
    public void SubmitsMetrics(string packageVersion)
    {
        SkipIfMLegacyQuartz(packageVersion);

        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);

        collector.Expect("Quartz");

        RunTestApplication(new TestSettings
        {
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private static void SkipIfMLegacyQuartz(string packageVersion)
    {
        if (!Quartz4Plus(packageVersion))
        {
            throw new SkipException("Quartz < 4.0.0. Metrics are not supported.");
        }
    }
#endif

    private static bool Quartz4Plus(string packageVersion)
    {
#if NET10_0_OR_GREATER
        return string.IsNullOrEmpty(packageVersion) || new Version(packageVersion) >= new Version(4, 0, 0);
#else
        return false;
#endif
    }
}
