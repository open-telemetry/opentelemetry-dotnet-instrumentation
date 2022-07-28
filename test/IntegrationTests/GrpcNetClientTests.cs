// <copyright file="GrpcNetClientTests.cs" company="OpenTelemetry Authors">
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

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class GrpcNetClientTests : TestHelper
{
    public GrpcNetClientTests(ITestOutputHelper output)
        : base("GrpcNetClient", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var agent = new MockZipkinCollector(Output);

        // Grpc.Net.Client is using various version of http communication under the hood.
        // Disabling HttpClient instrumentation to have consistent set of spans.
        EnvironmentHelper.CustomEnvironmentVariables.Add("OTEL_DOTNET_AUTO_TRACES_DISABLED_INSTRUMENTATIONS", "HttpClient");

        RunTestApplication(agent.Port);

        const int expectedSpansCount = 1;
        var spans = agent.WaitForSpans(expectedSpansCount, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            spans.Count.Should().Be(expectedSpansCount);

            spans.Count(s => s.Tags["otel.library.name"] == "OpenTelemetry.Instrumentation.GrpcNetClient").Should().Be(1);
        }
    }
}
