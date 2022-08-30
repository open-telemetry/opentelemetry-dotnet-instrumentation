// <copyright file="WcfCoreTests.cs" company="OpenTelemetry Authors">
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

// This test won't work outside of windows as it need the server side which is .NET Framework only.
#if NETCOREAPP3_1_OR_GREATER && _WINDOWS

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfCoreTests : TestHelper, IDisposable
{
    private const string ServiceName = "TestApplication.Wcf.Client.Core";
    private ProcessHelper _serverProcess;

    public WcfCoreTests(ITestOutputHelper output)
        : base("Wcf.Client.Core", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var agent = new MockZipkinCollector(Output);

        var serverHelper = new WcfServerTestHelper(Output);
        _serverProcess = serverHelper.RunWcfServer(agent.Port);

        RunTestApplication(agent.Port);

        // wait so the spans from server are delivered
        Task.Delay(2000);
        var spans = agent.WaitForSpans(4, TimeSpan.FromSeconds(5));

        using var scope = new AssertionScope();
        spans.Count.Should().Be(4);

        foreach (var span in spans)
        {
            span.Tags["otel.library.name"].Should().Be("OpenTelemetry.Instrumentation.Wcf");
        }
    }

    public void Dispose()
    {
        _serverProcess.Process.Kill();

        Output.WriteLine($"ProcessId: " + _serverProcess.Process.Id);
        Output.WriteLine($"Exit Code: " + _serverProcess.Process.ExitCode);
        Output.WriteResult(_serverProcess);
    }
}

#endif
