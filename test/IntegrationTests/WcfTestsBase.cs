// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Sockets;
using IntegrationTests.Helpers;
using Xunit.Abstractions;
using static OpenTelemetry.Proto.Trace.V1.Span.Types;

namespace IntegrationTests;

#pragma warning disable CA1515 // Consider making public types internal. Needed by xunit.
public abstract class WcfTestsBase : TestHelper, IDisposable
#pragma warning restore CA1515 // Consider making public types internal. Needed by xunit.
{
    private readonly string _testAppName;
    private ProcessHelper? _serverProcess;
    private bool _disposed;

    protected WcfTestsBase(string testAppName, ITestOutputHelper output)
        : base(testAppName, output)
    {
        _testAppName = testAppName;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            if (_serverProcess?.Process != null)
            {
                if (_serverProcess.Process.HasExited)
                {
                    Output.WriteLine($"WCF server process finished. Exit code: {_serverProcess.Process.ExitCode}.");
                }
                else
                {
                    _serverProcess.Process.Kill();
                    _serverProcess.Process.WaitForExit();
                }

                Output.WriteLine("ProcessId: " + _serverProcess.Process.Id);
                Output.WriteLine("Exit Code: " + _serverProcess.Process.ExitCode);
                Output.WriteResult(_serverProcess);
            }

            _serverProcess?.Dispose();
        }

        _disposed = true;
    }

    protected async Task SubmitsTracesInternal(string clientPackageVersion, WcfServerTestHelperBase wcfServerTestHelperBase)
    {
#if NET
        Assert.NotNull(wcfServerTestHelperBase);
#else
        if (wcfServerTestHelperBase == null)
        {
            throw new ArgumentNullException(nameof(wcfServerTestHelperBase));
        }
#endif
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        (_serverProcess, var tcpPort, var httpPort) = wcfServerTestHelperBase.RunWcfServer(collector);

        await WaitForServer(tcpPort).ConfigureAwait(false);

        RunTestApplication(new TestSettings
        {
            PackageVersion = clientPackageVersion,
            Arguments = $"--tcpPort {tcpPort} --httpPort {httpPort}"
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

    private async Task WaitForServer(int tcpPort)
    {
        using var tcpClient = new TcpClient();
        var retries = 0;

        Output.WriteLine("Waiting for WCF Server to open ports.");
        while (retries < 60)
        {
            try
            {
                await tcpClient.ConnectAsync("127.0.0.1", tcpPort).ConfigureAwait(false);
                Output.WriteLine("WCF Server is running.");
                return;
            }
#pragma warning disable CA1031 // Do not catch general exception types. Any exception means the operation failed.
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types. Any exception means the operation failed.
            {
                retries++;
                await Task.Delay(500).ConfigureAwait(false);
            }
        }

        Assert.Fail("WCF Server did not open the port.");
    }
}
