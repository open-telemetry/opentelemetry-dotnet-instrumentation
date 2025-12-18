// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RedisCollection.Name)]
public class StackExchangeRedisTests : TestHelper
{
    private readonly RedisFixture _redis;

    public StackExchangeRedisTests(ITestOutputHelper output, RedisFixture redis)
        : base("StackExchangeRedis", output)
    {
        _redis = redis;
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.StackExchangeRedis), MemberType = typeof(LibraryVersion))]
    public async Task SubmitsTraces(string packageVersion)
    {
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);
        const int spanCount = 8;
        for (var i = 0; i < spanCount; i++)
        {
            collector.Expect("OpenTelemetry.Instrumentation.StackExchangeRedis");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--redis {_redis.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }
}
#endif
