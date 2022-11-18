// <copyright file="TestServer.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using TestApplication.Http.NetFramework.Helpers;

namespace TestApplication.Http.NetFramework;

public class TestServer : IDisposable
{
    private static readonly ActivitySource MyActivitySource = new ActivitySource("TestApplication.Http.NetFramework", "1.0.0");

    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    public TestServer(string sufix)
    {
        Port = TcpPortProvider.GetOpenPort();

        _listener = new HttpListener();
        _listener.Start();
        var prefix = new UriBuilder("http", "localhost", Port, sufix).ToString();
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

                using (var activity = MyActivitySource.StartActivity("manual span"))
                {
                    activity?.SetTag("test_tag", "test_value");
                }

                // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                // (Setting content-length avoids that)
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
                // somethig unexpected happened
                // log instead of crashing the thread
                Console.WriteLine("[EXCEPTION]: {0}", ex.Message);
                Console.WriteLine(ex);
            }
        }
    }
}
