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

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RedisCollection.Name)]
public class StackExchangeRedisTests : TestHelper
{
    private const string ServiceName = "TestApplication.StackExchangeRedis";
    private readonly RedisFixture _redis;

    public StackExchangeRedisTests(ITestOutputHelper output, RedisFixture redis)
        : base("StackExchangeRedis", output)
    {
        _redis = redis;
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public void SubmitsTraces()
    {
        using var agent = new MockZipkinCollector(Output);

        RunTestApplication(agent.Port, arguments: $"--redis {_redis.Port}");

        const int expectedSpansCount = 8;

        var spans = agent.WaitForSpans(expectedSpansCount, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            spans.Count.Should().Be(expectedSpansCount);

            foreach (var span in spans)
            {
                span.Tags["db.system"].Should().Be("redis");
            }
        }
    }
}
#endif
