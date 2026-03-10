// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RabbitMqCollectionFixture.Name)]
public class RabbitMqTests : TestHelper
{
    // https://github.com/open-telemetry/semantic-conventions/blob/d515887174e20a3546e89df5cb5a306231e1424b/docs/messaging/rabbitmq.md

    // Required messaging attributes set by the instrumentation
    private const string MessagingSystemAttributeName = "messaging.system";
    private const string MessagingOperationAttributeName = "messaging.operation";
    private const string MessagingDestinationAttributeName = "messaging.destination.name";

    // Required RabbitMQ attributes set by the instrumentation
    private const string RabbitMqRoutingKeyAttributeName = "messaging.rabbitmq.destination.routing_key";
    private const string RabbitMqDeliveryTagAttributeName = "messaging.rabbitmq.delivery_tag";

    // Recommended messaging attributes set by the instrumentation
    private const string MessagingBodySizeAttributeName = "messaging.message.body.size";

    // Required network attributes set by the instrumentation
    private const string ServerAddressAttributeName = "server.address";
    private const string ServerPortAttributeName = "server.port";

    // Recommended network attributes set by the instrumentation
    private const string NetworkTypeAttributeName = "network.type";
    private const string NetworkPeerAddressAttributeName = "network.peer.address";
    private const string NetworkPeerPortAttributeName = "network.peer.port";

    private readonly RabbitMqFixture _rabbitMq;

    public RabbitMqTests(ITestOutputHelper output, RabbitMqFixture rabbitMq)
        : base("RabbitMq", output)
    {
        _rabbitMq = rabbitMq;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.RabbitMq), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        // Skip the test if fixture does not support current platform
        _rabbitMq.SkipIfUnsupportedPlatform();

        if (string.IsNullOrEmpty(packageVersion) || Version.Parse(packageVersion) >= new Version(7, 0, 0))
        {
            TestRabbitMq7Plus(packageVersion);
        }
        else
        {
            TestRabbitMqLegacy(packageVersion);
        }
    }

    private static bool ValidatePropagation(ICollection<MockSpansCollector.Collected> collected)
    {
        var producerSpans = collected.Where(span => span.Span.Kind == Span.Types.SpanKind.Producer).ToList();

        Assert.Equal(3, producerSpans.Count);

        return producerSpans.All(span => VerifySingleMatchingConsumerSpan(span));

        bool VerifySingleMatchingConsumerSpan(MockSpansCollector.Collected producerSpan)
        {
            return collected.Count(spans =>
                spans.Span.Kind == Span.Types.SpanKind.Consumer &&
                spans.Span.Links[0].TraceId == producerSpan.Span.TraceId &&
                spans.Span.Links[0].SpanId == producerSpan.Span.SpanId) == 1;
        }
    }

    private static bool ValidateBasicSpanAttributes(IReadOnlyCollection<KeyValue> attributes, string operationName)
    {
        var messagingSystem = attributes.Single(kv => kv.Key == MessagingSystemAttributeName).Value.StringValue;
        var messagingOperation = attributes.Single(kv => kv.Key == MessagingOperationAttributeName).Value.StringValue;
        var destinationName = attributes.Single(kv => kv.Key == MessagingDestinationAttributeName).Value.StringValue;
        var routingKey = attributes.Single(kv => kv.Key == RabbitMqRoutingKeyAttributeName).Value.StringValue;
        var bodySize = attributes.Single(kv => kv.Key == MessagingBodySizeAttributeName).Value.IntValue;

        return messagingSystem == "rabbitmq" &&
               messagingOperation == operationName &&
               destinationName == "amq.default" &&
               routingKey == "hello" &&
               bodySize == 13;
    }

    private void TestRabbitMq7Plus(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("RabbitMQ.Client.Publisher");
        collector.Expect("RabbitMQ.Client.Subscriber");

        RunTestApplication(new()
        {
            Arguments = $"--rabbitmq {_rabbitMq.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private void TestRabbitMqLegacy(string packageVersion)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", VersionHelper.AutoInstrumentationVersion, span => ValidateProducerSpan(span));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", VersionHelper.AutoInstrumentationVersion, span => ValidateProducerSpan(span));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", VersionHelper.AutoInstrumentationVersion, span => ValidateProducerSpan(span));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", VersionHelper.AutoInstrumentationVersion, span => ValidateConsumerSpan(span, "receive"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", VersionHelper.AutoInstrumentationVersion, span => ValidateConsumerSpan(span, "deliver"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", VersionHelper.AutoInstrumentationVersion, span => ValidateConsumerSpan(span, "deliver"));

        collector.ExpectCollected(collected => ValidatePropagation(collected));

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--rabbitmq {_rabbitMq.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private bool ValidateConsumerSpan(Span span, string operationName)
    {
        var deliveryTag = span.Attributes.SingleOrDefault(kv => kv.Key == RabbitMqDeliveryTagAttributeName)?.Value.StringValue;
        return span.Kind == Span.Types.SpanKind.Consumer &&
               span.Links.Count == 1 &&
               ValidateBasicSpanAttributes(span.Attributes, operationName) &&
               (operationName != "receive" || ValidateNetworkAttributes(span.Attributes)) &&
               !string.IsNullOrEmpty(deliveryTag);
    }

    private bool ValidateProducerSpan(Span span)
    {
        return span.Kind == Span.Types.SpanKind.Producer && ValidateBasicSpanAttributes(span.Attributes, "publish") && ValidateNetworkAttributes(span.Attributes);
    }

    private bool ValidateNetworkAttributes(IReadOnlyCollection<KeyValue> spanAttributes)
    {
        var serverAddress = spanAttributes.Single(kv => kv.Key == ServerAddressAttributeName).Value.StringValue;
        var serverPort = spanAttributes.Single(kv => kv.Key == ServerPortAttributeName).Value.IntValue;
        var networkType = spanAttributes.Single(kv => kv.Key == NetworkTypeAttributeName).Value.StringValue;
        var networkPeerAddress = spanAttributes.Single(kv => kv.Key == NetworkPeerAddressAttributeName).Value.StringValue;
        var networkPeerPort = spanAttributes.Single(kv => kv.Key == NetworkPeerPortAttributeName).Value.IntValue;

        return serverAddress == "localhost" &&
               serverPort == _rabbitMq.Port &&
               networkPeerAddress is "127.0.0.1" or "::1" &&
               networkPeerPort == _rabbitMq.Port &&
               networkType is "ipv4" or "ipv6";
    }
}
