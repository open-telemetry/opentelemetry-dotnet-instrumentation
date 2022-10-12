// <copyright file="MockZipkinCollector.NetFramework.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK

using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockZipkinCollector : MockZipkinCollectorBase, IDisposable
{
    private readonly TestHttpListener _listener;

    private MockZipkinCollector(ITestOutputHelper output, string host = "localhost")
        : base(output)
    {
        _listener = new(output, HandleHttpRequests, host, "/api/v2/spans/");
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public override int Port { get => _listener.Port; }

    public static async Task<MockZipkinCollector> Start(ITestOutputHelper output, string host = "localhost")
    {
        var collector = new MockZipkinCollector(output, host);

        var healthzResult = await collector._listener.VerifyHealthzAsync();

        if (!healthzResult)
        {
            collector.Dispose();
            throw new InvalidOperationException($"Cannot start {nameof(MockLogsCollector)}!");
        }

        return collector;
    }

    public void Dispose()
    {
        DisposeInternal();

        _listener.Dispose();
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        if (ShouldDeserializeTraces)
        {
            using (var reader = new StreamReader(ctx.Request.InputStream))
            {
                var json = reader.ReadToEnd();
                var headers = new NameValueCollection(ctx.Request.Headers);

                Deserialize(json, headers);
            }
        }

        // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
        // (Setting content-length avoids that)

        ctx.Response.ContentType = "application/json";
        var buffer = Encoding.UTF8.GetBytes("{}");
        ctx.Response.ContentLength64 = buffer.LongLength;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.Close();
    }
}

#endif
