// <copyright file="DomainNeutralTests.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
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

public class DomainNeutralTests : TestHelper
{
    public DomainNeutralTests(ITestOutputHelper output)
        : base("DomainNeutral", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        EnvironmentTools.IsWindowsAdministrator().Should().BeTrue();

        // Add the necessary assembly to the GAC so it can be loaded as domain-neutral.
        var instrumentationAssembly = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(),
            "bin",
            "tracer-home",
            "net462",
            "OpenTelemetry.AutoInstrumentation.dll");
        File.Exists(instrumentationAssembly).Should().BeTrue();
        using var gacEntry = new GacEntry(instrumentationAssembly);

        // GAC Entry is not immediately visible, give it some time to process in the background.
        System.Threading.Thread.Sleep(20_000);

        // Domain-neutral depends on strong named assemblies to work, leverage some assets from
        // strong name testing in the current test.
        var assemblyPath = GetTestAssemblyPath();
        var integrationsFile = Path.Combine(assemblyPath, "StrongNamedTestsIntegrations.json");
        File.Exists(integrationsFile).Should().BeTrue();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE", integrationsFile);

        using var agent = new MockZipkinCollector(Output);

        RunTestApplication(agent.Port);

        const int expectedSpansCount = 1;
        var spans = await agent.WaitForSpansAsync(expectedSpansCount, TimeSpan.FromSeconds(5));

        using (new AssertionScope())
        {
            spans.Count.Should().Be(expectedSpansCount);

            spans.Count(s => s.Tags["validation"] == "StrongNamedValidation").Should().Be(1);
        }
    }
}
#endif
