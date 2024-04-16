// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

var factory = new ConnectionFactory { HostName = "localhost", Port = int.Parse(GetRabbitMqPort(args)) };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

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

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var receivedBody = ea.Body.ToArray();
    var receivedMessage = Encoding.UTF8.GetString(receivedBody);
    Console.WriteLine($"Received: {receivedMessage}");
};

#if NETFRAMEWORK
await channel.BasicGetAsync("hello", true); // BasicConsumeAsync does not produce Activity for RabbitMQ.Client.Subscriber
#else
await channel.BasicConsumeAsync(queue: "hello", autoAck: true, consumer);
#endif

static string GetRabbitMqPort(string[] args)
{
    if (args.Length > 1)
    {
        return args[1];
    }

    return "5672";
}
