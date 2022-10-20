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

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace IntegrationTests;
public abstract class WcfTestsBase : TestHelper, IDisposable
{
    private ProcessHelper _serverProcess;

    protected WcfTestsBase(string testAppName, ITestOutputHelper output)
        : base(testAppName, output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", testAppName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var collector = await MockSpansCollector.Start(Output);
        // the test app makes 2 calls (therefore we exepct 4 spans)
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Server);
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Client);
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Server);
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Client);

        var serverHelper = new WcfServerTestHelper(Output);
        _serverProcess = serverHelper.RunWcfServer(collector.Port);
        await WaitForServer();

        RunTestApplication(otlpTraceCollectorPort: collector.Port);

        collector.AssertExpectations();
    }

    public void Dispose()
    {
        if (_serverProcess.Process.HasExited)
        {
            Output.WriteLine($"WCF server process finished. Exit code: {_serverProcess.Process.ExitCode}.");
        }
        else
        {
            _serverProcess.Process.Kill();
        }

        Output.WriteLine("ProcessId: " + _serverProcess.Process.Id);
        Output.WriteLine("Exit Code: " + _serverProcess.Process.ExitCode);
        Output.WriteResult(_serverProcess);
    }

    private async Task WaitForServer()
    {
        const int tcpPort = 9090;
        using var tcpClient = new TcpClient();
        var retries = 0;

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

                Output.WriteLine("Waiting for WCF Server to open ports.");
                await Task.Delay(500);
            }
        }

        Assert.Fail("WCF Server did not open the port.");
    }
}
