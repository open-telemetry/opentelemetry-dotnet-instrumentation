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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using IntegrationTests.Helpers.Mocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockZipkinCollector : IDisposable
{
    private static readonly TimeSpan DefaultSpanWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly object _syncRoot = new object();
    private readonly ITestOutputHelper _output;
    private readonly IWebHost _listener;

    private MockZipkinCollector(ITestOutputHelper output)
    {
        _output = output;

        _listener = new WebHostBuilder()
            .UseKestrel(options =>
                options.Listen(IPAddress.Loopback, 0)) // dynamic port
            .Configure(x =>
            {
                x.Map("/api/v2/spans", x =>
                {
                    x.Run(HandleHttpRequests);
                });

                x.Map("/healthz", x =>
                {
                    x.Run(HandleHealtz);
                });
            })
            .Build();
    }

    /// <summary>
    /// Gets or sets a value indicating whether to skip serialization of traces.
    /// </summary>
    public bool ShouldDeserializeTraces { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port
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

    /// <summary>
    /// Gets the filters used to filter out spans we don't want to look at for a test.
    /// </summary>
    public List<Func<IMockSpan, bool>> SpanFilters { get; private set; } = new List<Func<IMockSpan, bool>>();

    private IImmutableList<IMockSpan> Spans { get; set; } = ImmutableList<IMockSpan>.Empty;

    private IImmutableList<NameValueCollection> RequestHeaders { get; set; } = ImmutableList<NameValueCollection>.Empty;

    public static async Task<MockZipkinCollector> Start(ITestOutputHelper output)
    {
        var collector = new MockZipkinCollector(output);

        collector.Start();

        var healthzResult = await collector.VerifyHealthzAsync();

        if (!healthzResult)
        {
            collector.Dispose();
            throw new InvalidOperationException($"Cannot start {nameof(MockLogsCollector)}!");
        }

        return collector;
    }

    /// <summary>
    /// Wait for the given number of spans to appear.
    /// </summary>
    /// <param name="count">The expected number of spans.</param>
    /// <param name="timeout">The timeout</param>
    /// <param name="instrumentationLibrary">The integration we're testing</param>
    /// <param name="minDateTime">Minimum time to check for spans from</param>
    /// <param name="returnAllOperations">When true, returns every span regardless of operation name</param>
    /// <returns>The list of spans.</returns>
    public async Task<IImmutableList<IMockSpan>> WaitForSpansAsync(
        int count,
        TimeSpan? timeout = null,
        string instrumentationLibrary = null,
        DateTimeOffset? minDateTime = null,
        bool returnAllOperations = false)
    {
        timeout ??= DefaultSpanWaitTimeout;
        var deadline = DateTime.Now.Add(timeout.Value);
        var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

        IImmutableList<IMockSpan> relevantSpans = ImmutableList<IMockSpan>.Empty;

        // Use a do-while to ensure at least one attempt at reading the data already
        // received. This is helpful for negative tests, ie.: no spans generated, when
        // the process emitting the spans already finished.
        do
        {
            lock (_syncRoot)
            {
                relevantSpans =
                    Spans
                        .Where(s => SpanFilters.All(shouldReturn => shouldReturn(s)))
                        .Where(s => s.Start > minimumOffset)
                        .ToImmutableList();
            }

            if (relevantSpans.Count(s => instrumentationLibrary == null || s.Library == instrumentationLibrary) >= count)
            {
                break;
            }

            await Task.Delay(500);
        }
        while (DateTime.Now < deadline);

        if (!returnAllOperations)
        {
            relevantSpans =
                relevantSpans
                    .Where(s => instrumentationLibrary == null || s.Library == instrumentationLibrary)
                    .ToImmutableList();
        }

        return relevantSpans;
    }

    public void Start()
    {
        _listener.Start();
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            WriteOutput($"Shutting down. Total spans received: '{Spans.Count}'");
        }

        _listener.Dispose();
    }

    private async Task HandleHealtz(HttpContext ctx)
    {
        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{}");
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
                var zspans = JsonConvert.DeserializeObject<List<ZSpanMock>>(await reader.ReadToEndAsync());
                if (zspans != null)
                {
                    IList<IMockSpan> spans = zspans.ConvertAll(x => (IMockSpan)x);

                    lock (_syncRoot)
                    {
                        Spans = Spans.AddRange(spans);
                        RequestHeaders = RequestHeaders.Add(ctx.Request.Headers.Aggregate(
                            new NameValueCollection(),
                            (seed, current) =>
                            {
                                seed.Add(current.Key, current.Value);
                                return seed;
                            }));
                    }
                }
            }
        }

        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{}");
    }

    private Task<bool> VerifyHealthzAsync()
    {
        var healhtzEndpoint = $"http://localhost:{Port}/healthz";

        return HealthzHelper.TestHealtzAsync(healhtzEndpoint, nameof(MockLogsCollector), _output);
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockZipkinCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}

#endif
