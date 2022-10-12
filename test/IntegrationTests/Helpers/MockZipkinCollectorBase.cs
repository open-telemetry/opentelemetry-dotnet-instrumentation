// <copyright file="MockZipkinCollectorBase.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using IntegrationTests.Helpers.Mocks;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public abstract class MockZipkinCollectorBase
{
    protected static readonly TimeSpan DefaultSpanWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly object _syncRoot = new object();
    private readonly ITestOutputHelper _output;

    protected MockZipkinCollectorBase(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to skip serialization of traces.
    /// </summary>
    public bool ShouldDeserializeTraces { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public abstract int Port { get; }

    /// <summary>
    /// Gets the filters used to filter out spans we don't want to look at for a test.
    /// </summary>
    public List<Func<IMockSpan, bool>> SpanFilters { get; private set; } = new List<Func<IMockSpan, bool>>();

    protected IImmutableList<IMockSpan> Spans { get; set; } = ImmutableList<IMockSpan>.Empty;

    protected IImmutableList<NameValueCollection> RequestHeaders { get; set; } = ImmutableList<NameValueCollection>.Empty;

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

    protected void Deserialize(string json, NameValueCollection headers)
    {
        var zspans = JsonConvert.DeserializeObject<List<ZSpanMock>>(json);
        if (zspans != null)
        {
            IList<IMockSpan> spans = zspans.ConvertAll(x => (IMockSpan)x);

            lock (_syncRoot)
            {
                Spans = Spans.AddRange(spans);
                RequestHeaders = RequestHeaders.Add(headers);
            }
        }
    }

    protected void DisposeInternal()
    {
        lock (_syncRoot)
        {
            WriteOutput($"Shutting down. Total spans received: '{Spans.Count}'");
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockZipkinCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
