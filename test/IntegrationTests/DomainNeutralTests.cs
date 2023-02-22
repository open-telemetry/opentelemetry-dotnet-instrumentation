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
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class DomainNeutralTests : TestHelper
{
    public DomainNeutralTests(ITestOutputHelper output)
        : base("DomainNeutral.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        EnvironmentTools.IsWindowsAdministrator().Should().BeTrue();

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("TestApplication.StrongNamedValidation");

        // Add the necessary assembly to the GAC so it can be loaded as domain-neutral.
        var instrumentationAssembly = Path.Combine(
            EnvironmentTools.GetSolutionDirectory(),
            "bin",
            "tracer-home",
            "netfx",
            "OpenTelemetry.AutoInstrumentation.dll");
        File.Exists(instrumentationAssembly).Should().BeTrue();
        using var gacEntry = new GacEntry(instrumentationAssembly);

        // Domain-neutral depends on strong named assemblies to work, leverage some assets from
        // strong name testing in the current test.
        var assemblyPath = GetTestAssemblyPath();
        var integrationsFile = Path.Combine(assemblyPath, "StrongNamedTestsIntegrations.json");
        File.Exists(integrationsFile).Should().BeTrue();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE", integrationsFile);
        RunTestApplication();

        collector.AssertExpectations();
    }
}
#endif
