// <copyright file="KafkaTests.cs" company="OpenTelemetry Authors">
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

using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(KafkaCollection.Name)]
public class KafkaTests : TestHelper
{
    private const string KafkaInstrumentationScopeName = "OpenTelemetry.AutoInstrumentation.Kafka";
    // https://github.com/confluentinc/confluent-kafka-dotnet/blob/07de95ed647af80a0db39ce6a8891a630423b952/src/Confluent.Kafka/Offset.cs#L36C44-L36C44
    private const int InvalidOffset = -1001;

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

        // Failed produce attempts made before topic is created.
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateResultProcessingProduceExceptionSpan(span, topicName), "Failed Produce attempt with delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateProduceExceptionSpan(span, topicName), "Failed Produce attempt without delivery handler set.");
        collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Producer && ValidateResultProcessingProduceExceptionSpan(span, topicName), "Failed ProduceAsync attempt.");

        // For 1.4.0 null is returned when attempting to read from non-existent topic,
        // and no exception is thrown,
        // instrumentation creates no span in such case.
        if (packageVersion == string.Empty || Version.Parse(packageVersion) >= new Version(2, 3, 0))
        {
            // Failed consume attempt.
            collector.Expect(KafkaInstrumentationScopeName, span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumeExceptionSpan(span, topicName), "Failed Consume attempt.");
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
            Arguments = topicName
        });

        collector.AssertExpectations();
    }

    private static bool ValidateConsumeExceptionSpan(Span span, string topicName)
    {
        return ValidateConsumerSpan(span, topicName, InvalidOffset, null) &&
               span.Status.Code == Status.Types.StatusCode.Error;
    }

    private static bool ValidateConsumerSpan(Span span, string topicName, int messageOffset, string? expectedMessageKey = "testkey")
    {
        var kafkaMessageOffset = span.Attributes.Single(kv => kv.Key == "messaging.kafka.message.offset").Value.IntValue;
        var consumerGroupId = span.Attributes.Single(kv => kv.Key == "messaging.kafka.consumer.group").Value.StringValue;
        return ValidateCommonTags(span.Attributes, topicName, "rdkafka#consumer-2", "receive", 0, expectedMessageKey) &&
               kafkaMessageOffset == messageOffset &&
               consumerGroupId == $"test-consumer-group-{topicName}";
    }

    private static bool ValidateBasicProduceExceptionSpanExpectations(Span span, string topicName)
    {
        return ValidateCommonTags(span.Attributes, topicName, "rdkafka#producer-1", "publish", -1, "testkey") &&
               span.Status.Code == Status.Types.StatusCode.Error;
    }

    private static bool ValidateProduceExceptionSpan(Span span, string topicName)
    {
        return ValidateBasicProduceExceptionSpanExpectations(span, topicName) &&
               span.Attributes.Count(kv => kv.Key == "messaging.kafka.message.offset") == 0;
    }

    private static bool ValidateResultProcessingProduceExceptionSpan(Span span, string topicName)
    {
        // DeliveryResult processing results in offset being set.
        var offset = span.Attributes.SingleOrDefault(kv => kv.Key == "messaging.kafka.message.offset")?.Value.IntValue;
        return ValidateBasicProduceExceptionSpanExpectations(span, topicName) &&
               offset == InvalidOffset;
    }

    private static bool ValidateProducerSpan(Span span, string topicName, int partition, bool tombstoneExpected = false)
    {
        var isTombstone = span.Attributes.Single(kv => kv.Key == "messaging.kafka.message.tombstone").Value.BoolValue;

        return ValidateCommonTags(span.Attributes, topicName, "rdkafka#producer-1", "publish", partition, "testkey") &&
               isTombstone == tombstoneExpected &&
               span.Status is null;
    }

    private static bool ValidateCommonTags(IReadOnlyCollection<KeyValue> attributes, string topicName, string clientName, string operationName, int partition, string? expectedMessageKey)
    {
        var messagingSystem = attributes.Single(kv => kv.Key == "messaging.system").Value.StringValue;
        var messagingDestinationName = attributes.Single(kv => kv.Key == "messaging.destination.name").Value.StringValue;
        var messagingOperation = attributes.Single(kv => kv.Key == "messaging.operation").Value.StringValue;
        var messagingClientId = attributes.Single(kv => kv.Key == "messaging.client_id").Value.StringValue;
        var kafkaMessageKey = attributes.SingleOrDefault(kv => kv.Key == "messaging.kafka.message.key")?.Value.StringValue;
        var kafkaPartition = attributes.Single(kv => kv.Key == "messaging.kafka.destination.partition").Value.IntValue;

        return messagingSystem == "kafka" &&
               messagingDestinationName == topicName &&
               messagingOperation == operationName &&
               messagingClientId == clientName &&
               kafkaMessageKey == expectedMessageKey &&
               kafkaPartition == partition;
    }

    private static bool ValidatePropagation(ICollection<MockSpansCollector.Collected> collectedSpans, string topicName)
    {
        var expectedReceiveOperationName = $"{topicName} receive";
        var expectedPublishOperationName = $"{topicName} publish";
        var producerSpans = collectedSpans
            .Where(span =>
                span.Span.Name == expectedPublishOperationName &&
                !span.Span.Attributes.Single(attr => attr.Key == "messaging.kafka.message.tombstone").Value.BoolValue &&
                span.Span.Status is null);

        return producerSpans
            .Select(producerSpan =>
                GetMatchingConsumerSpan(collectedSpans, producerSpan.Span, expectedReceiveOperationName))
            .All(matchingConsumerSpan =>
                matchingConsumerSpan is not null);
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
