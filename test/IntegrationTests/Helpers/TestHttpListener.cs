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
using System.Threading;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class TestHttpListener : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly Action<HttpListenerContext> _requestHandler;
    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    public TestHttpListener(ITestOutputHelper output, Action<HttpListenerContext> requestHandler, string host = "localhost", string sufix = "/")
    {
        _output = output;
        _requestHandler = requestHandler;

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
                string prefix = new UriBuilder("http", host, Port, sufix).ToString();
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
            catch (Exception) when (!_listener.IsListening)
            {
                // we don't care about any exception when listener is stopped
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(TestHttpListener);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
