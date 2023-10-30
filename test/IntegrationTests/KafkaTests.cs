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

using Google.Protobuf.Collections;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(KafkaCollection.Name)]
public class KafkaTests : TestHelper
{
    public KafkaTests(ITestOutputHelper testOutputHelper)
        : base("Kafka", testOutputHelper)
    {
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [MemberData(nameof(LibraryVersion.Kafka), MemberType = typeof(LibraryVersion))]
    public void SubmitsTraces(string packageVersion)
    {
        var topicName = $"test-topic-{packageVersion}";

        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        collector.Expect("OpenTelemetry.AutoInstrumentation.Kafka", span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, -1), "Produced without delivery handler.");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Kafka", span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, 0), "Produced with delivery handler.");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Kafka", span => span.Kind == Span.Types.SpanKind.Producer && ValidateProducerSpan(span, topicName, -1, true), "Produced a tombstone.");

        collector.Expect("OpenTelemetry.AutoInstrumentation.Kafka", span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumerSpan(span, topicName, 0), "Consumed a first message.");
        collector.Expect("OpenTelemetry.AutoInstrumentation.Kafka", span => span.Kind == Span.Types.SpanKind.Consumer && ValidateConsumerSpan(span, topicName, 1), "Consumed a second message.");

        collector.ExpectCollected(collection => ValidatePropagation(collection, topicName));

        EnableBytecodeInstrumentation();

        RunTestApplication(new TestSettings
        {
            PackageVersion = packageVersion,
            Arguments = topicName
        });

        collector.AssertExpectations();
    }

    private static bool ValidateConsumerSpan(Span span, string topicName, int messageOffset)
    {
        var kafkaMessageOffset = span.Attributes.Single(kv => kv.Key == "messaging.kafka.message.offset").Value.IntValue;
        var consumerGroupId = span.Attributes.Single(kv => kv.Key == "messaging.kafka.consumer.group").Value.StringValue;
        return ValidateCommonTags(span.Attributes, topicName, "rdkafka#consumer-2", "process", 0) &&
               kafkaMessageOffset == messageOffset &&
               consumerGroupId == $"test-consumer-group-{topicName}";
    }

    private static bool ValidateProducerSpan(Span span, string topicName, int partition, bool tombstoneExpected = false)
    {
        var isTombstone = span.Attributes.Single(kv => kv.Key == "messaging.kafka.message.tombstone").Value.BoolValue;

        return ValidateCommonTags(span.Attributes, topicName, "rdkafka#producer-1", "publish", partition) &&
               isTombstone == tombstoneExpected;
    }

    private static bool ValidateCommonTags(IReadOnlyCollection<KeyValue> attributes, string topicName, string clientName, string operationName, int partition)
    {
        var messagingSystem = attributes.Single(kv => kv.Key == "messaging.system").Value.StringValue;
        var messagingDestinationName = attributes.Single(kv => kv.Key == "messaging.destination.name").Value.StringValue;
        var messagingOperation = attributes.Single(kv => kv.Key == "messaging.operation").Value.StringValue;
        var messagingClientId = attributes.Single(kv => kv.Key == "messaging.client_id").Value.StringValue;
        var kafkaMessageKey = attributes.Single(kv => kv.Key == "messaging.kafka.message.key").Value.StringValue;
        var kafkaPartition = attributes.Single(kv => kv.Key == "messaging.kafka.destination.partition").Value.IntValue;

        return messagingSystem == "kafka" &&
               messagingDestinationName == topicName &&
               messagingOperation == operationName &&
               messagingClientId == clientName &&
               kafkaMessageKey == "testkey" &&
               kafkaPartition == partition;
    }

    private static bool ValidatePropagation(ICollection<MockSpansCollector.Collected> collectedSpans, string topicName)
    {
        var expectedProcessOperationName = $"{topicName} process";
        var expectedPublishOperationName = $"{topicName} publish";
        var producerSpans = collectedSpans
            .Where(span =>
                span.Span.Name == expectedPublishOperationName &&
                !span.Span.Attributes.Single(attr => attr.Key == "messaging.kafka.message.tombstone").Value.BoolValue)
            .ToList();
        var firstProducerSpan = producerSpans[0].Span;
        var firstConsumerSpan = GetMatchingConsumerSpan(collectedSpans, firstProducerSpan, expectedProcessOperationName);
        var secondProducerSpan = producerSpans[1].Span;
        var secondConsumerSpan = GetMatchingConsumerSpan(collectedSpans, secondProducerSpan, expectedProcessOperationName);

        return firstProducerSpan.SpanId == firstConsumerSpan.Span.ParentSpanId &&
               secondProducerSpan.SpanId == secondConsumerSpan.Span.ParentSpanId;
    }

    private static MockSpansCollector.Collected GetMatchingConsumerSpan(ICollection<MockSpansCollector.Collected> arg, Span producerSpan, string expectedProcessOperationName)
    {
        return arg.Single(span => span.Span.Name == expectedProcessOperationName && span.Span.TraceId == producerSpan.TraceId);
    }
}
