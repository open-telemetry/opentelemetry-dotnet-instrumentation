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

#if NETCOREAPP3_1_OR_GREATER

using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
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

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        const int spanCount = 8;
        for (int i = 0; i < spanCount; i++)
        {
            collector.Expect("OpenTelemetry.Instrumentation.StackExchangeRedis");
        }

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--redis {_redis.Port}"
        });

        collector.AssertExpectations();
    }
}
#endif
