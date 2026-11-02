// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class KafkaInstrumentationTests
{
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
