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

#if NETFRAMEWORK
using Google.Protobuf;
using Google.Protobuf.Collections;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfNetFrameworkTests : WcfTestsBase
{
    private const int NetTcpPort = 9090;
    private const int HttpPort = 9009;
    private const string ExpectedChannelPath = "/Telemetry";
    private const string ExpectedPeerName = "127.0.0.1";

    public WcfNetFrameworkTests(ITestOutputHelper output)
        : base("Wcf.Client.NetFramework", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task SubmitsTraces()
    {
        using var collector = await SubmitsTracesInternal(string.Empty);
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 1");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.NetTcpChannelScheme, ExpectedChannelPath, ExpectedPeerName, NetTcpPort, WcfClientInstrumentation.NetTcpBindingMessageVersion) && WcfClientInstrumentation.ValidateSpanSuccessStatus(span), "Client 1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 2");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.NetTcpChannelScheme, ExpectedChannelPath, ExpectedPeerName, NetTcpPort, WcfClientInstrumentation.NetTcpBindingMessageVersion) && WcfClientInstrumentation.ValidateSpanSuccessStatus(span), "Client 2");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 3");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.NetTcpChannelScheme, ExpectedChannelPath, ExpectedPeerName, NetTcpPort, WcfClientInstrumentation.NetTcpBindingMessageVersion) && WcfClientInstrumentation.ValidateSpanSuccessStatus(span), "Client 3");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 4");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.HttpChannelScheme, ExpectedChannelPath, ExpectedPeerName, HttpPort, WcfClientInstrumentation.HttpBindingMessageVersion) && WcfClientInstrumentation.ValidateSpanSuccessStatus(span), "Client 4");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 5");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.HttpChannelScheme, ExpectedChannelPath, ExpectedPeerName, HttpPort, WcfClientInstrumentation.HttpBindingMessageVersion) && WcfClientInstrumentation.ValidateSpanSuccessStatus(span), "Client 5");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "Server 6");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.HttpChannelScheme, ExpectedChannelPath, ExpectedPeerName, HttpPort, WcfClientInstrumentation.HttpBindingMessageVersion) && WcfClientInstrumentation.ValidateSpanSuccessStatus(span), "Client 6");

        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom parent");
        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom sibling");

        collector.ExpectCollected(WcfClientInstrumentation.ValidateExpectedSpanHierarchy);

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTracesNoEndpoint()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => ValidateErrorSpanExpectations(span), "Client 1");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => ValidateErrorSpanExpectations(span), "Client 2");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Wcf", span => ValidateErrorSpanExpectations(span), "Client 3");

        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom parent");
        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom sibling");

        collector.ExpectCollected(WcfClientInstrumentation.ValidateExpectedSpanHierarchy);

        RunTestApplication(new TestSettings
        {
            PackageVersion = string.Empty
        });

        collector.AssertExpectations();
    }

    private static bool ValidateErrorSpanExpectations(Span span)
    {
        return WcfClientInstrumentation.ValidateBasicSpanExpectations(span, WcfClientInstrumentation.HttpChannelScheme, ExpectedChannelPath, ExpectedPeerName, HttpPort, WcfClientInstrumentation.HttpBindingMessageVersion) && span.Status.Code == Status.Types.StatusCode.Error;
    }
}

#endif
