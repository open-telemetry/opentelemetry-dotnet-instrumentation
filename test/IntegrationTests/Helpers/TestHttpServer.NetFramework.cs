// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK

using System.Net;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal sealed class TestHttpServer : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Action<HttpListenerContext> _requestHandler;
    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    public TestHttpServer(ITestOutputHelper output, Action<HttpListenerContext> requestHandler, string host, string sufix = "/")
    {
        _output = output;
        _requestHandler = requestHandler;

        Port = TcpPortProvider.GetOpenPort();

        _listener = new HttpListener();
        _listener.Start();
        var prefix = new UriBuilder("http", host, Port, sufix).ToString(); // See https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistenerprefixcollection.add?redirectedfrom=MSDN&view=net-6.0#remarks for info about the host value.
        _listener.Prefixes.Add(prefix);
        WriteOutput($"Listening on '{prefix}'");

        _listenerThread = new Thread(HandleHttpRequests);
        _listenerThread.Start();
    }

    /// <summary>
    /// Gets the TCP port that this listener is listening on.
    /// </summary>
    public int Port { get; }

    public static TestHttpServer CreateDefaultTestServer(ITestOutputHelper output)
    {
        return new TestHttpServer(
            output,
            context =>
        {
            context.Response.StatusCode = 200;
            context.Response.Close();
        },
            "localhost",
            "/test/");
    }

    public void Dispose()
    {
        WriteOutput($"Shutting down");
        _listener.Close();
        _listenerThread.Join();
    }

    private void HandleHttpRequests()
    {
        while (_listener.IsListening)
        {
            try
            {
                var ctx = _listener.GetContext();
                _requestHandler(ctx);
            }
            catch (HttpListenerException)
            {
                // listener was stopped,
                // ignore to let the loop end and the method return
            }
            catch (ObjectDisposedException)
            {
                // the response has been already disposed.
            }
            catch (InvalidOperationException)
            {
                // this can occur when setting Response.ContentLength64, with the framework claiming that the response has already been submitted
                // for now ignore, and we'll see if this introduces downstream issues
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception) when (!_listener.IsListening)
            {
                // we don't care about any exception when listener is stopped
            }
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // something unexpected happened
                // log instead of crashing the thread
                WriteOutput(ex.ToString());
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(TestHttpServer);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
#endif
