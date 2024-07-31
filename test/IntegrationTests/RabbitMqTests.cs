// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RabbitMqCollection.Name)]
public class RabbitMqTests : TestHelper
{
    private const string MessagingSystemAttributeName = "messaging.system";
    private const string MessagingOperationAttributeName = "messaging.operation";
    private const string MessagingDestinationAttributeName = "messaging.destination.name";
    private const string RabbitMqRoutingKeyAttributeName = "messaging.rabbitmq.destination.routing_key";
    private const string MessagingBodySizeAttributeName = "messaging.message.body.size";
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

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", span => ValidateProducerSpan(span));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", span => ValidateProducerSpan(span));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", span => ValidateProducerSpan(span));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", span => ValidateConsumerSpan(span, "receive"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", span => ValidateConsumerSpan(span, "process"));
        collector.Expect("OpenTelemetry.AutoInstrumentation.RabbitMq", span => ValidateConsumerSpan(span, "process"));

        collector.ExpectCollected(collected => ValidatePropagation(collected));

        EnableBytecodeInstrumentation();
        RunTestApplication(new()
        {
            Arguments = $"--rabbitmq {_rabbitMq.Port}",
            PackageVersion = packageVersion
        });

        collector.AssertExpectations();
    }

    private static bool ValidateConsumerSpan(Span span, string operationName)
    {
        return span.Kind == Span.Types.SpanKind.Consumer && span.Links.Count == 1 && ValidateBasicSpanAttributes(span.Attributes, operationName);
    }

    private static bool ValidateProducerSpan(Span span)
    {
        return span.Kind == Span.Types.SpanKind.Producer && ValidateBasicSpanAttributes(span.Attributes, "publish");
    }

    private static bool ValidatePropagation(ICollection<MockSpansCollector.Collected> collected)
    {
        var producerSpans = collected.Where(span => span.Span.Kind == Span.Types.SpanKind.Producer).ToList();

        producerSpans.Count.Should().Be(3);

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
}
