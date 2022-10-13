// <copyright file="MassTransitTests.cs" company="OpenTelemetry Authors">
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

#if !NETFRAMEWORK
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class MassTransitTests : TestHelper
{
    public MassTransitTests(ITestOutputHelper output)
        : base("MassTransit", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", "TestApplication.MassTransit");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var agent = await MockZipkinCollector.Start(Output);
        RunTestApplication(agent.Port);

        const int expectedSpans = 3;
        var spans = await agent.WaitForSpansAsync(expectedSpans);

        using (new AssertionScope())
        {
            spans.Count.Should().BeGreaterOrEqualTo(expectedSpans);

            foreach (var span in spans)
            {
                span.Library.Should().Be("MassTransit");
            }
        }
    }
}
#endif
