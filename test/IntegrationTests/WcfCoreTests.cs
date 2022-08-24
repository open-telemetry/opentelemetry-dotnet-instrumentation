// <copyright file="WcfCoreTests.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfCoreTests : TestHelper, IDisposable
{
    private const string ServiceName = "TestApplication.Wcf.Client.Core";
    private Process _serverProcess;

    public WcfCoreTests(ITestOutputHelper output)
        : base("Wcf.Client.Core", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var agent = new MockZipkinCollector(Output);

        var serverHelper = new WcfServerTestHelper(Output);
        _serverProcess = serverHelper.RunWcfServer(agent.Port);

        RunTestApplication(agent.Port);

        // wait so the spans from server are delivered
        Task.Delay(2000);
        var spans = agent.WaitForSpans(4, TimeSpan.FromSeconds(5));

        using var scope = new AssertionScope();
        spans.Count.Should().Be(4);

        foreach (var span in spans)
        {
            span.Resource.Should().Be("http://opentelemetry.io/StatusService/Ping");
            span.Tags["rpc.system"].Should().Be("wcf");
            span.Tags["rpc.service"].Should().Be("http://opentelemetry.io/StatusService");
            span.Tags["rpc.method"].Should().Be("Ping");
            span.Tags["wcf.channel.path"].Should().Be("/Telemetry");
        }

        var clientSpans = spans.Where(span => span.Service == "TestApplication.Wcf.Client.Core").ToList();
        var serverSpans = spans.Where(span => span.Service == "TestApplication.Wcf.Server.NetFramework").ToList();

        foreach (var clientSpan in clientSpans)
        {
            clientSpan.Tags["span.kind"].Should().Be("client");
            clientSpan.Tags["net.peer.name"].Should().Be("localhost");
            clientSpan.Tags["otel.library.name"].Should().Be("OpenTelemetry.Instrumentation.Wcf");
        }

        var httpClientSpan = clientSpans.Where(span => span.Tags["wcf.channel.scheme"] == "http").ToList()[0];
        var netTcpClientSpan = clientSpans.Where(span => span.Tags["wcf.channel.scheme"] == "net.tcp").ToList()[0];

        httpClientSpan.Tags["net.peer.port"].Should().Be("9009");
        httpClientSpan.Tags["peer.service"].Should().Be("localhost:9009");

        netTcpClientSpan.Tags["net.peer.port"].Should().Be("9090");
        netTcpClientSpan.Tags["peer.service"].Should().Be("localhost:9090");

        foreach (var serverSpan in serverSpans)
        {
            serverSpan.Tags["span.kind"].Should().Be("server");
            serverSpan.Tags["otel.library.name"].Should().Be("OpenTelemetry.WCF");
        }
    }

    public void Dispose()
    {
        _serverProcess?.Kill();
    }
}

#endif
