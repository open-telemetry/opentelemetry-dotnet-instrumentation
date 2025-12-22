// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;
using System.Text;
using TestApplication.Http.NetFramework.Helpers;

namespace TestApplication.Http.NetFramework;

internal sealed class TestServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    public TestServer(string suffix)
    {
        Port = TcpPortProvider.GetOpenPort();

        _listener = new HttpListener();
        _listener.Start();
        var prefix = new UriBuilder("http", "localhost", Port, suffix).ToString();
        _listener.Prefixes.Add(prefix);
        Console.WriteLine($"[LISTENER] Listening on '{prefix}'");

        _listenerThread = new Thread(HandleHttpRequests);
        _listenerThread.Start();
    }

    /// <summary>
    /// Gets the TCP port that this listener is listening on.
    /// </summary>
    public int Port { get; }

    public void Dispose()
    {
        Console.WriteLine($"[LISTENER] shutting down.");
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

                using var reader = new StreamReader(ctx.Request.InputStream);
                var request = reader.ReadToEnd();

                Console.WriteLine("[SERVER] Received: {0}", request);

                // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                // (Setting content-length avoids that)
                ctx.Response.Headers.Add("Custom-Response-Test-Header1", "Test-Value1");
                ctx.Response.Headers.Add("Custom-Response-Test-Header2", "Test-Value2");
                ctx.Response.Headers.Add("Custom-Response-Test-Header3", "Test-Value3");
                ctx.Response.ContentType = "text/plain";
                var buffer = Encoding.UTF8.GetBytes("Pong");
                ctx.Response.ContentLength64 = buffer.LongLength;
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                ctx.Response.Close();
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
            catch (Exception) when (!_listener.IsListening)
            {
                // we don't care about any exception when listener is stopped
            }
            catch (Exception ex)
            {
                // something unexpected happened
                // log instead of crashing the thread
                Console.WriteLine("[EXCEPTION]: {0}", ex.Message);
                Console.WriteLine(ex);
            }
        }
    }
}
