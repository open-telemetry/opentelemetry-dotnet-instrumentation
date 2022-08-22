// <copyright file="WcfNetFrameworkTests.cs" company="OpenTelemetry Authors">
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

#if NET462

using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using IntegrationTests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfNetFrameworkTests : TestHelper, IDisposable
{
    private const string ServiceName = "TestApplication.Wcf.Client.NetFramework";
    private Process _serverProcess;

    public WcfNetFrameworkTests(ITestOutputHelper output)
        : base("Wcf.Client.NetFramework", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var agent = new MockZipkinCollector(Output);

        var serverHelper = new WcfServerTestHelper(Output);
        _serverProcess = serverHelper.StartTestApplication(agent.Port, framework: "net462");

        RunTestApplication(agent.Port);
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
            span.Tags["otel.library.name"].Should().Be("OpenTelemetry.WCF");
        }

        var clientSpans = spans.Where(span => span.Service == "TestApplication.Wcf.Client.NetFramework").ToList();
        var serverSpans = spans.Where(span => span.Service == "TestApplication.Wcf.Server.NetFramework").ToList();

        foreach (var clientSpan in clientSpans)
        {
            clientSpan.Tags["span.kind"].Should().Be("client");
            clientSpan.Tags["net.peer.name"].Should().Be("localhost");
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
        }
    }

    public void Dispose()
    {
        _serverProcess?.Kill();
    }
}

#endif
