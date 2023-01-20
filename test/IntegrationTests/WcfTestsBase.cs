// <copyright file="WcfTestsBase.cs" company="OpenTelemetry Authors">
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

using System.Net.Sockets;
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace IntegrationTests;
public abstract class WcfTestsBase : TestHelper, IDisposable
{
    protected WcfTestsBase(string testAppName, ITestOutputHelper output)
        : base(testAppName, output)
    {
    }

    protected ProcessHelper? ServerProcess { get; set; }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        EnvironmentTools.IsWindowsAdministrator().Should().BeTrue(); // WCF Server needs admin

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        // the test app makes 2 calls (therefore we exepct 4 spans)
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Server, "Server 1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Client, "Client 1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Server, "Server 2");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Client, "Client 2");

        var serverHelper = new WcfServerTestHelper(Output);
        ServerProcess = serverHelper.RunWcfServer(collector);
        await WaitForServer();

        RunTestApplication();

        collector.AssertExpectations();
    }

    public void Dispose()
    {
        if (ServerProcess?.Process == null)
        {
            return;
        }

        if (ServerProcess.Process.HasExited)
        {
            Output.WriteLine($"WCF server process finished. Exit code: {ServerProcess.Process.ExitCode}.");
        }
        else
        {
            ServerProcess.Process.Kill();
        }

        Output.WriteLine("ProcessId: " + ServerProcess.Process.Id);
        Output.WriteLine("Exit Code: " + ServerProcess.Process.ExitCode);
        Output.WriteResult(ServerProcess);
    }

    protected async Task WaitForServer()
    {
        const int tcpPort = 9090;
        using var tcpClient = new TcpClient();
        var retries = 0;

        Output.WriteLine("Waiting for WCF Server to open ports.");
        while (retries < 60)
        {
            try
            {
                await tcpClient.ConnectAsync("127.0.0.1", tcpPort);
                Output.WriteLine("WCF Server is running.");
                return;
            }
            catch (Exception)
            {
                retries++;
                await Task.Delay(500);
            }
        }

        Assert.Fail("WCF Server did not open the port.");
    }
}
