// <copyright file="MultipleAppDomainsTests.cs" company="OpenTelemetry Authors">
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

public class MultipleAppDomainsTests : TestHelper
{
    public MultipleAppDomainsTests(ITestOutputHelper output)
        : base("MultipleAppDomains.NetFramework", output)
    {
    }

    [Fact(Skip = "Adding third-party integrations needs to be re-implemented after the native code update. See https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/2403")]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        const int expectedSpanCount = 5;
        for (var i = 0; i < expectedSpanCount; i++)
        {
            collector.Expect("TestApplication.StrongNamedValidation");
        }

        // Use the integrations file that bring the expected instrumentation.
        var assemblyPath = GetTestAssemblyPath();
        var integrationsFile = Path.Combine(assemblyPath, "StrongNamedTestsIntegrations.json");
        File.Exists(integrationsFile).Should().BeTrue();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE", integrationsFile);
        var (_, standardErrorOutput) = RunTestApplication();

        // Nothing regarding log should have been logged to the console.
        standardErrorOutput.Should().NotContain("Log:");

        collector.AssertExpectations();
    }
}
#endif
