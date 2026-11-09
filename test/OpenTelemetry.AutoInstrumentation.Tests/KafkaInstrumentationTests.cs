// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using NSubstitute;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class KafkaInstrumentationTests
{
    private const int MaxMessageKeyLength = 256;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProduceAsyncWithoutTaskStopsActivity(bool hasException)
    {
        using var activity = new Activity("ProduceAsync");
        activity.Start();
        var state = new CallTargetState(activity);
        var exception = hasException ? new InvalidOperationException("ProduceAsync failed before returning a task.") : null;
        var generator = CreateProduceAsyncContinuation();

        // Exercise the mapper that wraps the missing result in a duck-typed struct.
        var result = generator.SetContinuation(new object(), null, exception, in state);

        Assert.Null(result);
        Assert.True(activity.IsStopped);
        Assert.Equal(hasException ? ActivityStatusCode.Error : ActivityStatusCode.Unset, activity.Status);
        Assert.Null(activity.GetTagItem("messaging.kafka.destination.partition"));
        Assert.Null(activity.GetTagItem("messaging.kafka.message.offset"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProduceAsyncWithoutDeliveryResultStopsActivity(bool hasException)
    {
        using var activity = new Activity("ProduceAsync");
        activity.Start();
        var state = new CallTargetState(activity);
        var exception = new InvalidOperationException("ProduceAsync failed without a delivery result.");
        var task = hasException ? Task.FromException<DeliveryResult>(exception) : Task.FromResult<DeliveryResult>(null!);
        var generator = CreateProduceAsyncContinuation();

        if (hasException)
        {
            var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => generator.SetContinuation(new object(), task, null, in state)!);
            Assert.Same(exception, observed);
        }
        else
        {
            Assert.Null(await generator.SetContinuation(new object(), task, null, in state)!.ConfigureAwait(true));
        }

        Assert.True(activity.IsStopped);
        Assert.Equal(hasException ? ActivityStatusCode.Error : ActivityStatusCode.Unset, activity.Status);
        Assert.Null(activity.GetTagItem("messaging.kafka.destination.partition"));
        Assert.Null(activity.GetTagItem("messaging.kafka.message.offset"));
    }

    [Fact]
    public async Task ProduceAsyncRecordsDeliveryResult()
    {
        using var activity = new Activity("ProduceAsync");
        activity.Start();
        var state = new CallTargetState(activity);
        var deliveryResult = new DeliveryResult { Partition = new TestPartition(2), Offset = new TestOffset(42) };
        var task = Task.FromResult(deliveryResult);
        var generator = CreateProduceAsyncContinuation();

        Assert.Same(deliveryResult, await generator.SetContinuation(new object(), task, null, in state)!.ConfigureAwait(true));

        Assert.True(activity.IsStopped);
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
        Assert.Equal(2, activity.GetTagItem("messaging.kafka.destination.partition"));
        Assert.Equal(42L, activity.GetTagItem("messaging.kafka.message.offset"));
    }

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

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(MaxMessageKeyLength)]
    public void MessageKeyValueWithinMaximumLengthIsUnchanged(int length)
    {
        var key = new string('a', length);

        Assert.Same(key, KafkaInstrumentation.ExtractMessageKeyValue(key));
    }

    [Theory]
    [InlineData(MaxMessageKeyLength + 1)]
    [InlineData(900_000)]
    public void LongMessageKeyValueIsTruncated(int length)
    {
        var key = new string('a', length);

        var actual = KafkaInstrumentation.ExtractMessageKeyValue(key);

        Assert.Equal(new string('a', MaxMessageKeyLength), actual);
    }

    [Fact]
    public void MessageKeyTruncationDoesNotSplitSurrogatePair()
    {
        var prefix = new string('a', MaxMessageKeyLength - 1);
        var key = prefix + "\U0001F600" + new string('b', 10);

        Assert.Equal(prefix, KafkaInstrumentation.ExtractMessageKeyValue(key));
    }

    [Fact]
    public void MessageKeyTruncationPreservesCompleteSurrogatePair()
    {
        var prefix = new string('a', MaxMessageKeyLength - 2) + "\U0001F600";
        var key = prefix + new string('b', 10);

        Assert.Equal(prefix, KafkaInstrumentation.ExtractMessageKeyValue(key));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LongMessageKeyValueIsTruncatedOnSpans(bool isProducer)
    {
        var key = new string('a', 900_000);
        var message = Substitute.For<IKafkaMessage>();
        message.Key.Returns(key);

        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "OpenTelemetry.AutoInstrumentation.Kafka",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var partition = Substitute.For<ITopicPartition>();
        partition.Topic.Returns("test-topic");
        var client = Substitute.For<INamedClient>();

        using var activity = isProducer
            ? KafkaInstrumentation.StartProducerActivity(partition, message, client)
            : KafkaInstrumentation.StartConsumerActivity(client);
        Assert.NotNull(activity);

        if (!isProducer)
        {
            var consumeResult = Substitute.For<IConsumeResult>();
            consumeResult.Message.Returns(message);
            consumeResult.Topic = "test-topic";
            KafkaInstrumentation.EndConsumerActivity(activity, consumeResult);
        }

        var actual = Assert.IsType<string>(activity.GetTagItem("messaging.kafka.message.key"));
        Assert.Equal(new string('a', MaxMessageKeyLength), actual);
        Assert.Same(key, message.Key);
    }

    private static ContinuationGenerator<object, Task<DeliveryResult>> CreateProduceAsyncContinuation()
    {
        var generatorType = typeof(TaskContinuationGenerator<,,,>).MakeGenericType(
            typeof(ProducerProduceAsyncIntegration), typeof(object), typeof(Task<DeliveryResult>), typeof(DeliveryResult));
        return (ContinuationGenerator<object, Task<DeliveryResult>>)Activator.CreateInstance(generatorType)!;
    }

    private readonly struct TestPartition(int value)
    {
        public int Value { get; } = value;
    }

    private readonly struct TestOffset(long value)
    {
        public long Value { get; } = value;
    }

    private sealed class DeliveryResult
    {
        public TestPartition Partition { get; set; }

        public TestOffset Offset { get; set; }
    }
}
