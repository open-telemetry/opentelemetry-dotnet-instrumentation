// <copyright file="StackExchangeRedisTests.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER
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
    public void SubmitsTraces(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        var spanCount = packageVersion == string.Empty || Version.Parse(packageVersion) >= new Version(2, 1, 50) ? 8 : 4;
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
