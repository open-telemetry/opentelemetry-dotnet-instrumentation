// <copyright file="TestHttpListener.cs" company="OpenTelemetry Authors">
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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class TestHttpListener : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    private readonly object _requestHandlerLocker = new();
    private Action<HttpListenerContext> _requestHandler;

    private TestHttpListener(ITestOutputHelper output, string host, string sufix)
    {
        _output = output;

        // try up to 5 consecutive ports before giving up
        int retries = 4;
        while (true)
        {
            // seems like we can't reuse a listener if it fails to start,
            // so create a new listener each time we retry
            _listener = new HttpListener();

            try
            {
                _listener.Start();

                // See https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistenerprefixcollection.add?redirectedfrom=MSDN&view=net-6.0#remarks
                // for info about the host value.
                Port = TcpPortProvider.GetOpenPort();
                var prefix = new UriBuilder("http", host, Port, sufix).ToString();
                _listener.Prefixes.Add(prefix);

                // successfully listening
                _listenerThread = new Thread(HandleHttpRequests);
                _listenerThread.Start();
                WriteOutput($"Listening on '{prefix}'");

                return;
            }
            catch (HttpListenerException) when (retries > 0)
            {
                retries--;
            }

            // always close listener if exception is thrown,
            // whether it was caught or not
            _listener.Close();

            WriteOutput("Listener shut down. Could not find available port.");
        }
    }

    /// <summary>
    /// Gets the TCP port that this listener is listening on.
    /// </summary>
    public int Port { get; }

    public Action<HttpListenerContext> Handler
    {
        get
        {
            lock (_requestHandlerLocker)
            {
                return _requestHandler;
            }
        }

        set
        {
            lock (_requestHandlerLocker)
            {
                _requestHandler = value;
            }
        }
    }

    public static async Task<TestHttpListener> Start(ITestOutputHelper output, string host, string sufix = "/")
    {
        var listener = new TestHttpListener(output, host, sufix);
        var prefix = new UriBuilder("http", "localhost", listener.Port, sufix).ToString();
        bool running = await HealthzHelper.TestHealtzAsync($"{prefix}/healthz", nameof(TestHttpListener), output);
        if (running)
        {
            listener.Dispose();
            throw new InvalidOperationException($"Cannot start {nameof(TestHttpListener)}!");
        }

        return listener;
    }

    public void Dispose()
    {
        WriteOutput($"Listener is shutting down.");
        _listener.Stop();
    }

    private void HandleHttpRequests()
    {
        while (_listener.IsListening)
        {
            try
            {
                var ctx = _listener.GetContext();

                if (ctx.Request.RawUrl.EndsWith("/healthz", StringComparison.OrdinalIgnoreCase))
                {
                    SendResponse(ctx, HttpStatusCode.OK, "OK");
                    continue;
                }

                var handler = Handler;
                if (handler != null)
                {
                    handler(ctx);
                    continue;
                }

                SendResponse(ctx, HttpStatusCode.NotFound, "404 Not Found");
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
                // somethig unexpected happened
                // log instead of crashing the thread
                WriteOutput(ex.ToString());
            }
        }
    }

    private void SendResponse(HttpListenerContext ctx, HttpStatusCode httpStatusCode, string content)
    {
        ctx.Response.ContentType = "text/plain";
        var buffer = Encoding.UTF8.GetBytes(content);
        ctx.Response.ContentLength64 = buffer.LongLength;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.StatusCode = (int)httpStatusCode;
        ctx.Response.Close();
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(TestHttpListener);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
