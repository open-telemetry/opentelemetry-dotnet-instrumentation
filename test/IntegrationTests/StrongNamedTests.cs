// <copyright file="StrongNamedTests.cs" company="OpenTelemetry Authors">
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class StrongNamedTests : TestHelper
{
    public StrongNamedTests(ITestOutputHelper output)
        : base("StrongNamed", output)
    {
    }

    [Fact(Skip = "Bug in product. See: https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/1242")]
    public async Task SubmitsTraces()
    {
        var assemblyPath = GetTestAssemblyPath();
        var integrationsFile = Path.Combine(assemblyPath, "StrongNamedTestsIntegrations.json");
        File.Exists(integrationsFile).Should().BeTrue();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE", integrationsFile);

        using var agent = await MockZipkinCollector.Start(Output);

        RunTestApplication(agent.Port);

        const int expectedSpansCount = 1;
        var spans = await agent.WaitForSpansAsync(expectedSpansCount);

        using (new AssertionScope())
        {
            spans.Count.Should().Be(expectedSpansCount);

            spans.Count(s => s.Tags["validation"] == "StrongNamedValidation").Should().Be(1);
        }
    }
}
