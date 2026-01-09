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

    private readonly KafkaFixture _kafka;

    public KafkaTests(ITestOutputHelper testOutputHelper, KafkaFixture kafka)
        : base("Kafka", testOutputHelper)
    {
        _kafka = kafka;
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.Kafka), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        SkipIfUnsupportedPlatform(packageVersion);

        var topicName = $"test-topic-{packageVersion}";

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        // Failed produce attempts made before topic is created.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateResultProcessingProduceExceptionSpan(span, topicName), "Failed Produce attempt with delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProduceExceptionSpan(span, topicName), "Failed Produce attempt without delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateResultProcessingProduceExceptionSpan(span, topicName), "Failed ProduceAsync attempt.");

        if (packageVersion.Length == 0 || Version.Parse(packageVersion) != new Version(1, 4, 0))
        {
            // Failed consume attempt.
            collector.Expect(
                KafkaInstrumentationScopeName,
                span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumeExceptionSpan(span, topicName),
                "Failed Consume attempt.");
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

        collector.ExpectCollected(collection => ValidatePropagation(collection, topicName));
        EnableBytecodeInstrumentation();

        RunTestApplication(new TestSettings
        {
            PackageVersion = packageVersion,
            Arguments = $"--kafka {_kafka.Port} --topic-name {topicName}"
        });

        collector.AssertExpectations();
    }

    [SkippableTheory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    [MemberData(nameof(LibraryVersion.GetPlatformVersions), nameof(LibraryVersion.Kafka), MemberType = typeof(LibraryVersion))]
    // ReSharper disable once InconsistentNaming
    public void NoSpansForPartitionEOF(string packageVersion)
    {
        SkipIfUnsupportedPlatform(packageVersion);

        var topicName = $"test-topic2-{packageVersion}";

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        EnableBytecodeInstrumentation();

        RunTestApplication(new TestSettings
        {
            PackageVersion = packageVersion,
            Arguments = $"--kafka {_kafka.Port} --topic-name {topicName} --consume-only"
        });

        collector.AssertEmpty(TimeSpan.FromSeconds(10));
    }

    private static void SkipIfUnsupportedPlatform(string packageVersion)
    {
        if (!string.IsNullOrEmpty(packageVersion) && Version.Parse(packageVersion) < new Version(1, 9, 2) && EnvironmentTools.IsMacOS() && EnvironmentTools.IsArm64())
        {
            throw new SkipException("Confluent.Kafka < 1.9.2 is not supported on macOS ARM64.");
        }
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
        return ValidateBasicProduceExceptionSpan(span, topicName) && span.Attributes.All(kv => kv.Key != KafkaMessageOffsetAttributeName);
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

        return producerSpans.Select(
            producerSpan =>
                GetMatchingConsumerSpan(collectedSpans, producerSpan.Span, expectedReceiveOperationName))
            .All(consumerSpan => consumerSpan is not null);
    }

    private static MockSpansCollector.Collected? GetMatchingConsumerSpan(ICollection<MockSpansCollector.Collected> collectedSpans, Span producerSpan, string expectedReceiveOperationName)
    {
        return collectedSpans
            .SingleOrDefault(span =>
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
