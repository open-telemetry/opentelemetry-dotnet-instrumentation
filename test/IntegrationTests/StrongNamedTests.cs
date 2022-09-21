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

    [Fact]
    public async Task SubmitsTraces()
    {
        SetEnvironmentVariable("LONG_RUNNING", "true");

        var assemblyPath = GetTestAssemblyPath();
        var integrationsFile = Path.Combine(assemblyPath, "StrongNamedTestsIntegrations.json");
        File.Exists(integrationsFile).Should().BeTrue();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_INTEGRATIONS_FILE", integrationsFile);

        using var agent = new MockZipkinCollector(Output);

        using var process = StartTestApplication(agent.Port);
        using var helper = new ProcessHelper(process);

        const int expectedSpansCount = 1;
        var spans = await agent.WaitForSpansAsync(expectedSpansCount);

        try
        {
            using (new AssertionScope())
            {
                spans.Count.Should().Be(expectedSpansCount);

                spans.Count(s => s.Tags["validation"] == "StrongNamedValidation").Should().Be(1);
            }
        }
        finally
        {
            if (!helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();
            }

            Output.WriteLine("ProcessId: " + helper.Process.Id);
            Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
            Output.WriteResult(helper);
        }
    }
}
