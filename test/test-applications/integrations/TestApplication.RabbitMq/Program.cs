// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TestApplication.Shared;

namespace TestApplication.RabbitMq;

internal static class Program
{
#if RABBITMQ_7_0_0_OR_GREATER
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        ConsoleHelper.WriteSplashScreen(args);

        var factory = new ConnectionFactory { HostName = "localhost", Port = int.Parse(GetRabbitMqPort(args)) };
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        Console.WriteLine(channel.GetType().FullName);

        await channel.QueueDeclareAsync(
            queue: "hello",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        const string message = "Hello World!";
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: "hello",
            body: body,
            mandatory: false);
        Console.WriteLine($"Sent: {message}");

        var consumer = new AsyncEventingBasicConsumer(channel);

        using var resetEvent = new ManualResetEventSlim(false);

        consumer.ReceivedAsync += (model, ea) =>
        {
            var receivedBody = ea.Body.ToArray();
            var receivedMessage = Encoding.UTF8.GetString(receivedBody);
            Console.WriteLine($"Received: {receivedMessage}");
            resetEvent.Set();
            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queue: "hello", autoAck: true, consumer);

        resetEvent.Wait(TimeSpan.FromSeconds(5));
    }

    private static string GetRabbitMqPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "5672";
    }
#else
    private const string RoutingKey = "hello";
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(10);
    private static int _messageNumber;

    // based on tutorials from https://www.rabbitmq.com/tutorials/

    public static int Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

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

        var consumer = new TestSyncConsumer(syncConsumersModel);

        using var mre = new ManualResetEventSlim(false);

        consumer.Received += (_, ea) =>
        {
            Console.WriteLine("[x] Handling BasicDeliver in TestSyncConsumer.");
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
        var asyncConsumer = new TestAsyncConsumer(asyncConsumersModel);

        using var mre = new ManualResetEventSlim(false);

        asyncConsumer.Received += async (_, ea) =>
        {
            Console.WriteLine("[x] Handling BasicDeliver in TestAsyncConsumer.");
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

    private static string GetTestMessage()
    {
        return $"Hello World!{_messageNumber++}";
    }

    private static string GetRabbitMqPort(string[] args)
    {
        if (args.Length > 1)
        {
            return args[1];
        }

        return "5672";
    }

    // Custom consumer classes, with implementation (simplified) based on EventingBasicConsumer/AsyncEventingBasicConsumer
    // from the library.
    private class TestAsyncConsumer : AsyncDefaultBasicConsumer
    {
        public TestAsyncConsumer(IModel model)
            : base(model)
        {
        }

        public event AsyncEventHandler<BasicDeliverEventArgs>? Received;

        public override Task? HandleBasicDeliver(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            return Received?.Invoke(
                this,
                new BasicDeliverEventArgs(
                    consumerTag,
                    deliveryTag,
                    redelivered,
                    exchange,
                    routingKey,
                    properties,
                    body));
        }
    }

    private class TestSyncConsumer : DefaultBasicConsumer
    {
        public TestSyncConsumer(IModel model)
            : base(model)
        {
        }

        public event EventHandler<BasicDeliverEventArgs>? Received;

        public override void HandleBasicDeliver(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IBasicProperties properties,
            ReadOnlyMemory<byte> body)
        {
            base.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
            Received?.Invoke(this, new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body));
        }
    }
#endif
}
