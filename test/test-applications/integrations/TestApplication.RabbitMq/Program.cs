// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TestApplication.RabbitMq;

internal static class Program
{
    private const string RoutingKey = "hello";
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(10);
    private static int _messageNumber;

    // based on tutorials from https://www.rabbitmq.com/tutorials/

    public static int Main(string[] args)
    {
        var result = PublishAndConsumeWithSyncDispatcher(args);
        if (result != 0)
        {
            return result;
        }

        return PublishAndConsumeAsyncWithAsyncDispatcher(args);
    }

    private static int PublishAndConsumeWithSyncDispatcher(string[] args)
    {
        var syncConsumersConnectionFactory = new ConnectionFactory { HostName = "localhost", Port = int.Parse(GetRabbitMqPort(args)) };
        using var syncConsumersConnection = syncConsumersConnectionFactory.CreateConnection();
        using var syncConsumersModel = syncConsumersConnection.CreateModel();

        syncConsumersModel.QueueDeclare(
            queue: RoutingKey,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        Publish(syncConsumersModel, GetTestMessage(), null);

        for (var i = 0; i < 5; i++)
        {
            var basicGetResult = syncConsumersModel.BasicGet(queue: RoutingKey, true);
            if (basicGetResult is not null)
            {
                ProcessReceivedMessage(basicGetResult.Body);
                break;
            }

            Console.WriteLine($"Read nothing, retrying: {i}");
        }

        Publish(syncConsumersModel, GetTestMessage(), syncConsumersModel.CreateBasicProperties());

        var consumer = new EventingBasicConsumer(syncConsumersModel);

        using var mre = new ManualResetEventSlim(false);

        consumer.Received += (_, ea) =>
        {
            ProcessReceivedMessage(ea.Body);
            mre.Set();
        };

        var consumerTag = syncConsumersModel.BasicConsume(RoutingKey, true, consumer);
        if (!mre.Wait(DefaultWaitTimeout))
        {
            Console.WriteLine("Timed-out waiting for callback to complete.");
            return 1;
        }

        syncConsumersModel.BasicCancel(consumerTag);
        return 0;
    }

    private static int PublishAndConsumeAsyncWithAsyncDispatcher(string[] args)
    {
        var asyncConsumersConnectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = int.Parse(GetRabbitMqPort(args)),
            DispatchConsumersAsync = true
        };
        using var asyncConsumersConnection = asyncConsumersConnectionFactory.CreateConnection();
        using var asyncConsumersModel = asyncConsumersConnection.CreateModel();

        Publish(asyncConsumersModel, GetTestMessage(), asyncConsumersModel.CreateBasicProperties());
        var asyncConsumer = new AsyncEventingBasicConsumer(asyncConsumersModel);

        using var mre = new ManualResetEventSlim(false);

        asyncConsumer.Received += async (_, ea) =>
        {
            ProcessReceivedMessage(ea.Body);
            await Task.Yield();
            mre.Set();
        };

        var asyncConsumerTag = asyncConsumersModel.BasicConsume(RoutingKey, true, asyncConsumer);

        if (!mre.Wait(DefaultWaitTimeout))
        {
            Console.WriteLine("Timed-out waiting for callback to complete.");
            return 1;
        }

        asyncConsumersModel.BasicCancel(asyncConsumerTag);

        return 0;
    }

    private static void ProcessReceivedMessage(ReadOnlyMemory<byte> messageBody)
    {
        var receivedMessageContent = Encoding.UTF8.GetString(messageBody.ToArray());
        Console.WriteLine($" [x] Received {receivedMessageContent}");
    }

    private static void Publish(IModel channel, string message, IBasicProperties? basicProperties)
    {
        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: RoutingKey,
            basicProperties: basicProperties,
            body: Encoding.UTF8.GetBytes(message));
        Console.WriteLine($" [x] Sent {message}");
    }

    private static string GetRabbitMqPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "5672";
    }

    private static string GetTestMessage()
    {
        return $"Hello World!{_messageNumber++}";
    }
}
