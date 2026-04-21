// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RedisCollectionFixture.Name)]
public class StackExchangeRedisTests(ITestOutputHelper output, RedisFixture redis) : TestHelper("StackExchangeRedis", output)
{
    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.StackExchangeRedis), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        const int spanCount = 8;
        for (var i = 0; i < spanCount; i++)
        {
            collector.Expect("OpenTelemetry.Instrumentation.StackExchangeRedis");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--redis {redis.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
