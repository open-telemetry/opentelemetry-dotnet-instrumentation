// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(KafkaCollection.Name)]
public class KafkaTests : TestHelper
{
    private const string MessagingPublishOperationAttributeValue = "publish";
    private const string MessagingReceiveOperationAttributeValue = "receive";
    private const string MessagingSystemAttributeName = "messaging.system";
    private const string MessagingOperationAttributeName = "messaging.operation";
    private const string MessagingDestinationAttributeName = "messaging.destination.name";
    private const string MessagingClientIdAttributeName = "messaging.client_id";

    private const string KafkaMessageSystemAttributeValue = "kafka";
    private const string KafkaConsumerGroupAttributeName = "messaging.kafka.consumer.group";
    private const string KafkaMessageKeyAttributeName = "messaging.kafka.message.key";
    private const string KafkaMessageKeyAttributeValue = "testkey";
    private const string KafkaDestinationPartitionAttributeName = "messaging.kafka.destination.partition";
    private const string KafkaMessageOffsetAttributeName = "messaging.kafka.message.offset";
    private const string KafkaMessageTombstoneAttributeName = "messaging.kafka.message.tombstone";
    private const string KafkaInstrumentationScopeName = "OpenTelemetry.AutoInstrumentation.Kafka";
    private const string KafkaProducerClientIdAttributeValue = "rdkafka#producer-1";
    private const string KafkaConsumerClientIdAttributeValue = "rdkafka#consumer-2";

    // https://github.com/confluentinc/confluent-kafka-dotnet/blob/07de95ed647af80a0db39ce6a8891a630423b952/src/Confluent.Kafka/Offset.cs#L36C44-L36C44
    private const int InvalidOffset = -1001;
    private const string TestApplicationInstrumentationScopeName = "TestApplication.Kafka";

    public KafkaTests(ITestOutputHelper testOutputHelper)
        : base("Kafka", testOutputHelper)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Kafka), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        var topicName = $"test-topic-{packageVersion}";

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect(TestApplicationInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Internal);

        // Failed produce attempts made before topic is created.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateResultProcessingProduceExceptionSpan(span, topicName), "Failed Produce attempt with delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProduceExceptionSpan(span, topicName), "Failed Produce attempt without delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateResultProcessingProduceExceptionSpan(span, topicName), "Failed ProduceAsync attempt.");

        if (packageVersion == string.Empty || Version.Parse(packageVersion) >= new Version(2, 3, 0))
        {
            // Failed consume attempt.
            collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumeExceptionSpan(span, topicName), "Failed Consume attempt.");
        }
        else
        {
            // For 1.4.0 null is returned when attempting to read from non-existent topic,
            // and no exception is thrown.
            collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateEmptyReadConsumerSpan(span, topicName), "Successful Consume attempt that returned no message.");
        }

        // Successful produce attempts after topic was created with admin client.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, 0), "Successful ProduceAsync attempt.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, -1), "Successful Produce attempt without delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, 0), "Successful Produce attempt with delivery handler set.");

        // Successful produce attempt after topic was created for tombstones.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, -1, true), "Successful sync Publish attempt with a tombstone.");

        // Successful consume attempts.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumerSpan(span, topicName, 0), "First successful Consume attempt.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumerSpan(span, topicName, 1), "Second successful Consume attempt.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumerSpan(span, topicName, 2), "Third successful Consume attempt.");

        // Consume attempt that returns no message.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateEmptyReadConsumerSpan(span, topicName), "Additional successful attempt after all the produced messages were already read.");

        collector.ExpectCollected(collection => ValidatePropagation(collection, topicName));

        EnableBytecodeInstrumentation();

        RunTestApplication(new TestSettings
        {
            PackageVersion = packageVersion,
            Arguments = topicName
        });

        collector.AssertExpectations();
    }

    private static bool ValidateEmptyReadConsumerSpan(Span span, string topicName)
    {
        var consumerGroupId = span.Attributes.Single(kv => kv.Key == KafkaConsumerGroupAttributeName).Value.StringValue;
        return span.Name == MessagingReceiveOperationAttributeValue &&
               span.Links.Count == 0 &&
               ValidateBasicSpanAttributes(span.Attributes, KafkaConsumerClientIdAttributeValue, MessagingReceiveOperationAttributeValue) &&
               consumerGroupId == GetConsumerGroupIdAttributeValue(topicName);
    }

    private static string GetConsumerGroupIdAttributeValue(string topicName)
    {
        return $"test-consumer-group-{topicName}";
    }

    private static bool ValidateConsumeExceptionSpan(Span span, string topicName)
    {
        return ValidateConsumerSpan(span, topicName, InvalidOffset, null) &&
               span.Status.Code == Status.Types.StatusCode.Error;
    }

    private static bool ValidateConsumerSpan(Span span, string topicName, int messageOffset, string? expectedMessageKey = KafkaMessageKeyAttributeValue)
    {
        var kafkaMessageOffset = span.Attributes.SingleOrDefault(kv => kv.Key == KafkaMessageOffsetAttributeName)?.Value.IntValue;
        var consumerGroupId = span.Attributes.Single(kv => kv.Key == KafkaConsumerGroupAttributeName).Value.StringValue;
        return ValidateCommonAttributes(span.Attributes, topicName, KafkaConsumerClientIdAttributeValue, MessagingReceiveOperationAttributeValue, 0, expectedMessageKey) &&
               kafkaMessageOffset == messageOffset &&
               consumerGroupId == GetConsumerGroupIdAttributeValue(topicName);
    }

    private static bool ValidateBasicProduceExceptionSpan(Span span, string topicName)
    {
        return ValidateCommonAttributes(span.Attributes, topicName, KafkaProducerClientIdAttributeValue, MessagingPublishOperationAttributeValue, -1, KafkaMessageKeyAttributeValue) &&
               span.Status.Code == Status.Types.StatusCode.Error;
    }

    private static bool ValidateProduceExceptionSpan(Span span, string topicName)
    {
        return ValidateBasicProduceExceptionSpan(span, topicName) &&
               span.Attributes.Count(kv => kv.Key == KafkaMessageOffsetAttributeName) == 0;
    }

    private static bool ValidateResultProcessingProduceExceptionSpan(Span span, string topicName)
    {
        // DeliveryResult processing results in offset being set.
        var offset = span.Attributes.SingleOrDefault(kv => kv.Key == KafkaMessageOffsetAttributeName)?.Value.IntValue;
        return ValidateBasicProduceExceptionSpan(span, topicName) &&
               offset == InvalidOffset;
    }

    private static bool ValidateProducerSpan(Span span, string topicName, int partition, bool tombstoneExpected = false)
    {
        var isTombstone = span.Attributes.Single(kv => kv.Key == KafkaMessageTombstoneAttributeName).Value.BoolValue;

        return ValidateCommonAttributes(span.Attributes, topicName, KafkaProducerClientIdAttributeValue, MessagingPublishOperationAttributeValue, partition, KafkaMessageKeyAttributeValue) &&
               isTombstone == tombstoneExpected &&
               span.Status is null;
    }

    private static bool ValidateCommonAttributes(IReadOnlyCollection<KeyValue> attributes, string topicName, string clientId, string operationName, int partition, string? expectedMessageKey)
    {
        var messagingDestinationName = attributes.SingleOrDefault(kv => kv.Key == MessagingDestinationAttributeName)?.Value.StringValue;
        var kafkaMessageKey = attributes.SingleOrDefault(kv => kv.Key == KafkaMessageKeyAttributeName)?.Value.StringValue;
        var kafkaPartition = attributes.SingleOrDefault(kv => kv.Key == KafkaDestinationPartitionAttributeName)?.Value.IntValue;

        return ValidateBasicSpanAttributes(attributes, clientId, operationName) &&
               messagingDestinationName == topicName &&
               kafkaMessageKey == expectedMessageKey &&
               kafkaPartition == partition;
    }

    private static bool ValidateBasicSpanAttributes(IReadOnlyCollection<KeyValue> attributes, string clientId, string operationName)
    {
        var messagingSystem = attributes.Single(kv => kv.Key == MessagingSystemAttributeName).Value.StringValue;
        var messagingOperation = attributes.Single(kv => kv.Key == MessagingOperationAttributeName).Value.StringValue;
        var messagingClientId = attributes.Single(kv => kv.Key == MessagingClientIdAttributeName).Value.StringValue;

        return messagingSystem == KafkaMessageSystemAttributeValue &&
               messagingOperation == operationName &&
               messagingClientId == clientId;
    }

    private static bool ValidatePropagation(ICollection<MockSpansCollector.Collected> collectedSpans, string topicName)
    {
        var expectedReceiveOperationName = $"{topicName} {MessagingReceiveOperationAttributeValue}";
        var expectedPublishOperationName = $"{topicName} {MessagingPublishOperationAttributeValue}";
        var producerSpans = collectedSpans
            .Where(span =>
                span.Span.Name == expectedPublishOperationName &&
                !span.Span.Attributes.Single(attr => attr.Key == KafkaMessageTombstoneAttributeName).Value.BoolValue &&
                span.Span.Status is null);

        var consumerSpansWithLinks = producerSpans
            .Select(producerSpan =>
                GetMatchingConsumerSpan(collectedSpans, producerSpan.Span, expectedReceiveOperationName))
            .ToList();

        var manuallyCreatedSpan = collectedSpans.Single(collectedSpan =>
            collectedSpan.InstrumentationScopeName == TestApplicationInstrumentationScopeName);

        var consumerSpanWithCustomParent = consumerSpansWithLinks.SingleOrDefault(
            span =>
                span.Span.TraceId == manuallyCreatedSpan.Span.TraceId &&
                span.Span.ParentSpanId == manuallyCreatedSpan.Span.SpanId);

        var consumerSpansHaveCreationContextAsParent =
            consumerSpansWithLinks
                .Except(new[] { consumerSpanWithCustomParent })
                .All(consumerSpan =>
                    ValidateCreationContextAsParent(consumerSpan));

        return consumerSpanWithCustomParent is not null && consumerSpansHaveCreationContextAsParent;
    }

    private static bool ValidateCreationContextAsParent(MockSpansCollector.Collected? consumerSpan)
    {
        return consumerSpan?.Span.TraceId == consumerSpan?.Span.Links[0].TraceId &&
               consumerSpan?.Span.ParentSpanId == consumerSpan?.Span.Links[0].SpanId;
    }

    private static MockSpansCollector.Collected GetMatchingConsumerSpan(ICollection<MockSpansCollector.Collected> collectedSpans, Span producerSpan, string expectedReceiveOperationName)
    {
        return collectedSpans
            .Single(span =>
            {
                var parentLinksCount = span.Span.Links.Count(
                    link =>
                        link.TraceId == producerSpan.TraceId &&
                        link.SpanId == producerSpan.SpanId);
                return span.Span.Name == expectedReceiveOperationName &&
                       parentLinksCount == 1;
            });
    }
}
