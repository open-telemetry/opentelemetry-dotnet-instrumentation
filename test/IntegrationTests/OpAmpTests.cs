// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using IntegrationTests.Helpers;
using OpAmp.Proto.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

public class OpAmpTests : TestHelper
{
    public OpAmpTests(ITestOutputHelper output)
#if NET
        : base("Http", output)
#else
        : base("Http.NetFramework", output)
#endif
    {
    }

    [Fact]
    public void OpAmpClient_CanConnect()
    {
        using var server = new MockOpAmpServer(Output);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{server.Port}/v1/opamp");
        SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "opamp.test=true");

        AgentDescription? agentDescriptionFrame = null;

        server.Expect(
            f =>
            {
                agentDescriptionFrame = f.AgentDescription;
                return f.AgentDescription != null;
            },
            "Has AgentDescription frame");

        server.Expect(f => f.AgentDisconnect != null, "Has AgentDisconnect frame");

        RunTestApplication();

        server.AssertExpectations();

        Assert.NotNull(agentDescriptionFrame);
        Assert.Contains(agentDescriptionFrame.NonIdentifyingAttributes, a => a.Key == "opamp.test");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OpAmpClient_DoesNotCollectInternalTransportTraces()
    {
        using var server = new MockOpAmpServer(Output);
        using var collector = new MockSpansCollector(Output);

        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{server.Port}/v1/opamp");

#if NET
        collector.Expect(
            "System.Net.Http",
            span => span.Kind == Span.Types.SpanKind.Client && !IsOpAmpTransportSpan(span, server.Port),
            "Has application HTTP client span");
        collector.Expect(
            "Microsoft.AspNetCore",
            span => span.Kind == Span.Types.SpanKind.Server,
            "Has application ASP.NET Core server span");
        collector.Expect(
            "TestApplication.Http",
            span => span.Name == "manual span",
            "Has application manual span");
#else
        collector.Expect(
            "OpenTelemetry.Instrumentation.Http.HttpWebRequest",
            span => span.Kind == Span.Types.SpanKind.Client && !IsOpAmpTransportSpan(span, server.Port),
            "Has application HTTP client span");
#endif
        collector.ExpectAllCollected(collected => collected.All(span => !IsOpAmpTransportSpan(span.Span, server.Port)));

        server.Expect(f => f.AgentDescription != null, "Has AgentDescription frame");
        server.Expect(f => f.AgentDisconnect != null, "Has AgentDisconnect frame");

        RunTestApplication();

        server.AssertExpectations();
        collector.AssertExpectations();
    }

    private static bool IsOpAmpTransportSpan(Span span, int serverPort)
    {
        return span.Kind == Span.Types.SpanKind.Client &&
               HasOpAmpEndpointAttribute(span) &&
               HasServerPortAttribute(span, serverPort);
    }

    private static bool HasOpAmpEndpointAttribute(Span span)
    {
        return Contains(span.Name, "/v1/opamp") ||
               span.Attributes.Any(attribute => Contains(attribute.Value.StringValue, "/v1/opamp"));
    }

    private static bool HasServerPortAttribute(Span span, int serverPort)
    {
        var serverPortString = serverPort.ToString(CultureInfo.InvariantCulture);

        return span.Attributes.Any(attribute =>
            (attribute.Key == "server.port" && attribute.Value.IntValue == serverPort) ||
            Contains(attribute.Value.StringValue, $":{serverPortString}/v1/opamp"));
    }

    private static bool Contains(string value, string expected)
    {
#if NET
        return value.Contains(expected, StringComparison.OrdinalIgnoreCase);
#else
        return value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
#endif
    }
}
