// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Sockets;
using System.Runtime.InteropServices;
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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !SendSigTerm(_serverProcess.Process.Id))
                    {
                        _serverProcess.Process.Kill();
                    }

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
        Assert.True(!EnvironmentTools.IsWindows() || EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");
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

        _serverProcess = wcfServerTestHelperBase.RunWcfServer(collector);

        await WaitForServer().ConfigureAwait(false);

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

    [DllImport("libc", SetLastError = true)]
#pragma warning disable CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1300 // Element should begin with upper-case letter
    private static extern int kill(int pid, int sig);
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore SA1204 // Static elements should appear before instance elements
#pragma warning restore CA5392 // Use DefaultDllImportSearchPaths attribute for P/Invokes

    private static bool SendSigTerm(int processId)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            const int sigTerm = 15;
            var result = kill(processId, sigTerm);

            return result == 0;
        }

        return false;
    }
}
