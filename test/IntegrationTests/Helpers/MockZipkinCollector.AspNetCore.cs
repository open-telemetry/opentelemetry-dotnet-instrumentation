// <copyright file="MockZipkinCollector.AspNetCore.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockZipkinCollector : MockZipkinCollectorBase, IDisposable
{
    private readonly IWebHost _listener;

    private MockZipkinCollector(ITestOutputHelper output)
        : base(output)
    {
        _listener = new WebHostBuilder()
            .UseKestrel(options =>
                options.Listen(IPAddress.Loopback, 0)) // dynamic port
            .Configure(x => x.Map("/api/v2/spans", x =>
                {
                    x.Run(HandleHttpRequests);
                }))
            .Build();
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public override int Port
    {
        get
        {
            string address = _listener.ServerFeatures
                .Get<IServerAddressesFeature>()
                .Addresses
                .First();
            int port = int.Parse(address.Split(':').Last());

            return port;
        }
    }

    public static async Task<MockZipkinCollector> Start(ITestOutputHelper output)
    {
        var collector = new MockZipkinCollector(output);
        await collector._listener.StartAsync();

        return collector;
    }

    public void Dispose()
    {
        DisposeInternal();

        _listener.Dispose();
    }

    private async Task HandleHttpRequests(HttpContext ctx)
    {
        if (ShouldDeserializeTraces)
        {
            if (!ctx.Request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                ctx.Request.EnableBuffering();
            }

            ctx.Request.Body.Position = 0;

            using (var reader = new StreamReader(ctx.Request.Body))
            {
                var json = await reader.ReadToEndAsync();
                var headers = ctx.Request.Headers.Aggregate(new NameValueCollection(), (seed, current) =>
                {
                    seed.Add(current.Key, current.Value);
                    return seed;
                });

                Deserialize(json, headers);
            }
        }

        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{}");
    }
}

#endif
