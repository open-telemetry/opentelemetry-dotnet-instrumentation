// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class KafkaInstrumentationTests
{
    private const string SourceName = "OpenTelemetry.AutoInstrumentation.Kafka";

    [Theory]
    [InlineData("abc")]
    [InlineData(int.MaxValue)]
    [InlineData(uint.MaxValue)]
    [InlineData(long.MaxValue)]
    [InlineData(ulong.MaxValue)]
    [InlineData(float.MaxValue)]
    [InlineData(double.MaxValue)]
    public void MessageKeyValueIsExtractedForBasicType(object value)
    {
        Assert.Equal(Convert.ToString(value, CultureInfo.InvariantCulture), KafkaInstrumentation.ExtractMessageKeyValue(value));
    }

    [Fact]
    public void MessageKeyValueIsExtractedForDecimal()
    {
        decimal input = decimal.MaxValue;
        Assert.Equal(Convert.ToString(input, CultureInfo.InvariantCulture), KafkaInstrumentation.ExtractMessageKeyValue(input));
    }

    [Fact]
    public void MessageKeyValueIsNotExtractedForUnrecognizedType()
    {
        var value = new byte[] { 1, 2, 3 };
        Assert.Null(KafkaInstrumentation.ExtractMessageKeyValue(value));
    }

    [Fact]
    public void ProducerActivityUsesSemanticConventionsV1440()
    {
        using var listener = CreateListener();

        var topicPartition = new TopicPartitionStub("my-topic", 3);
        var message = new KafkaMessageStub("my-key", "my-value");
        var producer = new NamedClientStub("rdkafka#producer-1");

        using var activity = KafkaInstrumentation.StartProducerActivity(topicPartition, message, producer);

        Assert.NotNull(activity);
        Assert.Equal("send my-topic", activity.DisplayName);
        Assert.Equal("send", activity.GetTagItem("messaging.operation.name"));
        Assert.Equal("send", activity.GetTagItem("messaging.operation.type"));
        Assert.Equal("kafka", activity.GetTagItem("messaging.system"));
        Assert.Equal("my-topic", activity.GetTagItem("messaging.destination.name"));
        Assert.Equal("rdkafka#producer-1", activity.GetTagItem("messaging.client.id"));
        Assert.Equal("3", activity.GetTagItem("messaging.destination.partition.id"));
        Assert.Equal("my-key", activity.GetTagItem("messaging.kafka.message.key"));
    }

    [Fact]
    public void ConsumerActivityUsesSemanticConventionsV1440()
    {
        using var listener = CreateListener();

        var consumer = new NamedClientStub("rdkafka#consumer-2");
        ConsumerCache.Add(consumer, "my-group");

        using var activity = KafkaInstrumentation.StartConsumerActivity(consumer);
        Assert.NotNull(activity);

        var consumeResult = new ConsumeResultStub("my-topic", partition: 1, offset: 42, message: new KafkaMessageStub("my-key", "my-value"));
        KafkaInstrumentation.EndConsumerActivity(activity, consumeResult);

        Assert.Equal("receive my-topic", activity.DisplayName);
        Assert.Equal("receive", activity.GetTagItem("messaging.operation.name"));
        Assert.Equal("receive", activity.GetTagItem("messaging.operation.type"));
        Assert.Equal("kafka", activity.GetTagItem("messaging.system"));
        Assert.Equal("my-topic", activity.GetTagItem("messaging.destination.name"));
        Assert.Equal("rdkafka#consumer-2", activity.GetTagItem("messaging.client.id"));
        Assert.Equal("my-group", activity.GetTagItem("messaging.consumer.group.name"));
        Assert.Equal("1", activity.GetTagItem("messaging.destination.partition.id"));
        Assert.Equal(42L, activity.GetTagItem("messaging.kafka.offset"));
    }

    [Fact]
    public void DeliveryResultsUseSemanticConventionsV1440()
    {
        using var listener = CreateListener();
        using var source = new ActivitySource("test-source");
        using var activity = source.StartActivity("test");
        Assert.NotNull(activity);

        KafkaInstrumentation.SetDeliveryResults(activity, new DeliveryResultStub(partition: 7, offset: 99));

        Assert.Equal("7", activity.GetTagItem("messaging.destination.partition.id"));
        Assert.Equal(99L, activity.GetTagItem("messaging.kafka.offset"));
    }

    [Fact]
    public void ActivitySourceDeclaresSemanticConventionsV1440SchemaUrl()
    {
        using var listener = CreateListener();
        using var activity = KafkaInstrumentation.StartProducerActivity(
            new TopicPartitionStub("my-topic", 0),
            new KafkaMessageStub("my-key", "my-value"),
            new NamedClientStub("rdkafka#producer-1"));

        Assert.NotNull(activity);
        Assert.Equal("https://opentelemetry.io/schemas/1.44.0", activity.Source.TelemetrySchemaUrl);
    }

    [Fact]
    public void ErrorSpansCarryErrorType()
    {
        using var listener = CreateListener();
        using var source = new ActivitySource("test-source");
        using var activity = source.StartActivity("test");
        Assert.NotNull(activity);

        KafkaInstrumentation.SetError(activity, new InvalidOperationException("boom"));

        Assert.Equal("System.InvalidOperationException", activity.GetTagItem("error.type"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void ErrorTypeUsesCanonicalNameForGenericExceptions()
    {
        // Confluent.Kafka reports produce failures as ProduceException<TKey, TValue>. The mangled
        // FullName of a generic type is not the canonical class name the conventions ask for.
        using var listener = CreateListener();
        using var source = new ActivitySource("test-source");
        using var activity = source.StartActivity("test");
        Assert.NotNull(activity);

        KafkaInstrumentation.SetError(activity, new GenericFailureStub<string, int>());

        Assert.Equal(
            "OpenTelemetry.AutoInstrumentation.Tests.KafkaInstrumentationTests+GenericFailureStub",
            activity.GetTagItem("error.type"));
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name is SourceName or "test-source",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }

#pragma warning disable CA1032, RCS1194 // Test-only exception; standard constructors are not needed.
    private sealed class GenericFailureStub<TKey, TValue> : Exception
    {
    }
#pragma warning restore CA1032, RCS1194

    private sealed class NamedClientStub : INamedClient
    {
        public NamedClientStub(string name) => Name = name;

        public string Name { get; }
    }

    private sealed class TopicPartitionStub : ITopicPartition
    {
        public TopicPartitionStub(string? topic, int partition)
        {
            Topic = topic;
            Partition = new Partition { Value = partition };
        }

        public string? Topic { get; }

        public Partition Partition { get; }
    }

    private sealed class KafkaMessageStub : IKafkaMessage
    {
        public KafkaMessageStub(object? key, object? value)
        {
            Key = key;
            Value = value;
        }

        public object? Key { get; }

        public object? Value { get; set; }

        public IHeaders? Headers { get; set; }
    }

    private sealed class ConsumeResultStub : IConsumeResult
    {
        public ConsumeResultStub(string? topic, int partition, long offset, IKafkaMessage? message)
        {
            Topic = topic;
            Partition = new Partition { Value = partition };
            Offset = new Offset { Value = offset };
            Message = message;
        }

        public IKafkaMessage? Message { get; }

        public string? Topic { get; set; }

        public Offset Offset { get; set; }

        public Partition Partition { get; set; }

        public bool IsPartitionEOF { get; set; }
    }

    private sealed class DeliveryResultStub : IDeliveryResult
    {
        public DeliveryResultStub(int partition, long offset)
        {
            Partition = new Partition { Value = partition };
            Offset = new Offset { Value = offset };
        }

        public Partition Partition { get; set; }

        public Offset Offset { get; set; }
    }
}
