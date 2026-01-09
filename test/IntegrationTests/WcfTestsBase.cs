// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Sockets;
using IntegrationTests.Helpers;
using Xunit.Abstractions;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace IntegrationTests;

public abstract class WcfTestsBase : TestHelper, IDisposable
{
    private readonly string _testAppName;
    private ProcessHelper? _serverProcess;

    protected WcfTestsBase(string testAppName, ITestOutputHelper output)
        : base(testAppName, output)
    {
        _testAppName = testAppName;
    }

    public void Dispose()
    {
        if (_serverProcess?.Process == null)
        {
            return;
        }

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

    protected async Task SubmitsTracesInternal(string clientPackageVersion, WcfServerTestHelperBase wcfServerTestHelperBase)
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        _serverProcess = wcfServerTestHelperBase.RunWcfServer(collector);

        await WaitForServer();

        RunTestApplication(new TestSettings
        {
            PackageVersion = clientPackageVersion
        });

        collector.Expect(wcfServerTestHelperBase.ServerInstrumentationScopeName, span => span.Kind == SpanKind.Server, "Server 1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Client, "Client 1");
        collector.Expect(wcfServerTestHelperBase.ServerInstrumentationScopeName, span => span.Kind == SpanKind.Server, "Server 2");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == SpanKind.Client, "Client 2");

        collector.Expect($"TestApplication.{_testAppName}", span => span.Kind == SpanKind.Internal, "Custom parent");
        collector.Expect($"TestApplication.{_testAppName}", span => span.Kind == SpanKind.Internal, "Custom sibling");

        collector.ExpectCollected(WcfClientInstrumentation.ValidateExpectedSpanHierarchy);

        collector.AssertExpectations();
    }

    private async Task WaitForServer()
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
