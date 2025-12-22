// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace TestApplication.Kafka;

internal static class Program
{
    private const string MessageKey = "testkey";
    private static string _bootstrapServers = "localhost:9092";

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 4)
        {
            throw new ArgumentException("Required parameters not provided.");
        }

        var port = args[1];
        _bootstrapServers = $"localhost:{port}";

        var topicName = args[3];

        if (args.Length == 5 && args[4] == "--consume-only")
        {
            return await ConsumeOnly(topicName).ConfigureAwait(false);
        }

        if (args.Length == 4)
        {
            return await ProduceAndConsume(topicName).ConfigureAwait(false);
        }

        throw new ArgumentException("Invalid parameters.");
    }

    private static async Task<int> ConsumeOnly(string topicName)
    {
        await CreateTopic(_bootstrapServers, topicName).ConfigureAwait(false);

        using var consumer = BuildConsumer(topicName, _bootstrapServers);
        consumer.Subscribe(topicName);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var consumeResult = consumer.Consume(cts.Token);
        if (consumeResult.IsPartitionEOF)
        {
            return 0;
        }

        Console.WriteLine("Unexpected Consume result.");
        return 1;
    }

    private static async Task<int> ProduceAndConsume(string topicName)
    {
        using var waitEvent = new ManualResetEventSlim();

        using var producer = BuildProducer(_bootstrapServers);

        // Attempts are made to produce messages to non-existent topic.
        // Intention is to verify exception handling logic
        // in ProduceProducerSyncIntegration, ProduceProducerAsyncIntegration
        // and ProducerDeliveryHandlerActionIntegration.
        // Note: order of calls seems to be important to test
        // exception handling in all of the mentioned classes.
        TryProduceSyncWithDeliveryHandler(producer, topicName, report =>
        {
            Console.WriteLine(
                $"Delivery report received, message offset: {report.Offset.Value}, error: {report.Error.IsError}.");
            waitEvent.Set();
        });
        await TryProduceAsync(producer, topicName).ConfigureAwait(false);
        TryProduceSync(producer, topicName);

        using var consumer = BuildConsumer(topicName, _bootstrapServers);
        consumer.Subscribe(topicName);

        TryConsumeMessage(consumer);

        Console.WriteLine("Waiting on delivery handler.");
        if (!waitEvent.Wait(TimeSpan.FromSeconds(10)))
        {
            Console.WriteLine("Timed-out waiting for delivery handler to complete.");
            return 1;
        }

        Console.WriteLine("Delivery handler completed.");

        await CreateTopic(_bootstrapServers, topicName).ConfigureAwait(false);
        Console.WriteLine("Topic creation completed.");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        // Required as first few Produce attempts may still fail
        // with "unknown topic" error
        // after topic creation completed.
        await WaitForSuccessfulProduceAsync(producer, topicName, cts.Token).ConfigureAwait(false);

        producer.Produce(topicName, CreateTestMessage());
        producer.Produce(
            topicName,
            CreateTestMessage(),
            report =>
            {
                Console.WriteLine(
                    $"Delivery report received, message offset: {report.Offset.Value}, error: {report.Error.IsError}.");
            });
        producer.Flush(cts.Token);

        // Consume all the produced messages.
        consumer.Consume(cts.Token);
        consumer.Consume(cts.Token);

        consumer.Consume(cts.Token);

        // Produce a tombstone.
        producer.Produce(topicName, new Message<string, string> { Key = MessageKey, Value = null! });
        return 0;
    }

    private static void TryProduceSync(IProducer<string, string> producer, string topicName)
    {
        try
        {
            producer.Produce(topicName, CreateTestMessage());
        }
        catch (ProduceException<string, string> e)
        {
            Console.WriteLine($"Produce sync without delivery handler exception: {e.Error.Reason}");
        }
    }

    private static void TryConsumeMessage(IConsumer<string, string> consumer)
    {
        try
        {
            var cr = consumer.Consume(TimeSpan.FromSeconds(5));
            Console.WriteLine($"Consumption succeeded, message received: {cr?.Message}");
        }
        catch (ConsumeException ex)
        {
            Console.WriteLine($"Consumption exception: {ex.Error.Reason}");
        }
    }

    private static async Task WaitForSuccessfulProduceAsync(IProducer<string, string> producer, string topicName, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                await producer.ProduceAsync(topicName, CreateTestMessage(), token).ConfigureAwait(false);
                return;
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"ProduceAsync exception: {ex.Error.Reason}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }
    }

    private static IProducer<string, string> BuildProducer(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        };
        return new ProducerBuilder<string, string>(config).Build();
    }

    private static IConsumer<string, string> BuildConsumer(string topicName, string bootstrapServers)
    {
        var conf = new ConsumerConfig
        {
            GroupId = $"test-consumer-group-{topicName}",
            BootstrapServers = bootstrapServers,
            // https://github.com/confluentinc/confluent-kafka-dotnet/tree/07de95ed647af80a0db39ce6a8891a630423b952#basic-consumer-example
            AutoOffsetReset = AutoOffsetReset.Earliest,
            CancellationDelayMaxMs = 5000,
            EnablePartitionEof = true,
            EnableAutoCommit = true
        };

        return new ConsumerBuilder<string, string>(conf).Build();
    }

    private static async Task TryProduceAsync(
        IProducer<string, string> producer,
        string topicName)
    {
        try
        {
            await producer.ProduceAsync(topicName, CreateTestMessage()).ConfigureAwait(false);
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($"ProduceAsync exception: {ex.Error.Reason}");
        }
    }

    private static Message<string, string> CreateTestMessage()
    {
        return new Message<string, string> { Key = MessageKey, Value = "test" };
    }

    private static void TryProduceSyncWithDeliveryHandler(IProducer<string, string> producer, string topic, Action<DeliveryReport<string, string>> deliveryHandler)
    {
        try
        {
            producer.Produce(
                topic,
                CreateTestMessage(),
                deliveryHandler);
        }
        catch (ProduceException<string, string> e)
        {
            Console.WriteLine($"Produce sync with delivery handler exception: {e.Error.Reason}");
        }
    }

    private static async Task CreateTopic(string bootstrapServers, string topic)
    {
        var adminClientConfig = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
        await adminClient.CreateTopicsAsync([
            new TopicSpecification { Name = topic, ReplicationFactor = 1, NumPartitions = 1 }
        ]).ConfigureAwait(false);
    }
}
