// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !RABBITMQ_7_0_0_OR_GREATER
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TestApplication.RabbitMq;

// This custom consumer exercises instrumentation of an explicit implementation
// of RabbitMQ.Client.IAsyncBasicConsumer.HandleBasicDeliver.
internal sealed class AsyncExplicitImplementationConsumer : IBasicConsumer, IAsyncBasicConsumer
{
    private readonly AsyncDefaultBasicConsumer _consumer;

    public AsyncExplicitImplementationConsumer(IModel model)
    {
        _consumer = new AsyncDefaultBasicConsumer(model);
        Model = model;
    }

    public event EventHandler<BasicDeliverEventArgs>? Received;

    event AsyncEventHandler<ConsumerEventArgs> IAsyncBasicConsumer.ConsumerCancelled
    {
        add => _consumer.ConsumerCancelled += value;
        remove => _consumer.ConsumerCancelled -= value;
    }

    event EventHandler<ConsumerEventArgs> IBasicConsumer.ConsumerCancelled
    {
        add => throw new InvalidOperationException("Should never be called.");
        remove => throw new InvalidOperationException("Should never be called.");
    }

    public IModel Model { get; }

    Task IAsyncBasicConsumer.HandleBasicCancel(string consumerTag) => _consumer.HandleBasicCancel(consumerTag);

    Task IAsyncBasicConsumer.HandleBasicCancelOk(string consumerTag) => _consumer.HandleBasicCancelOk(consumerTag);

    Task IAsyncBasicConsumer.HandleBasicConsumeOk(string consumerTag) => _consumer.HandleBasicConsumeOk(consumerTag);

    Task IAsyncBasicConsumer.HandleBasicDeliver(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IBasicProperties properties,
#if RABBITMQ_6_0_0_OR_GREATER
        ReadOnlyMemory<byte> body)
#else
        byte[] body)
#endif
    {
        Received?.Invoke(this, new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body));
        return Task.CompletedTask;
    }

    Task IAsyncBasicConsumer.HandleModelShutdown(object model, ShutdownEventArgs reason) => _consumer.HandleModelShutdown(model, reason);

    void IBasicConsumer.HandleBasicCancel(string consumerTag) => throw new InvalidOperationException("Should never be called.");

    void IBasicConsumer.HandleBasicCancelOk(string consumerTag) => throw new InvalidOperationException("Should never be called.");

    void IBasicConsumer.HandleBasicConsumeOk(string consumerTag) => throw new InvalidOperationException("Should never be called.");

    void IBasicConsumer.HandleBasicDeliver(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IBasicProperties properties,
#if RABBITMQ_6_0_0_OR_GREATER
        ReadOnlyMemory<byte> body)
#else
        byte[] body)
#endif
        => throw new InvalidOperationException("Should never be called.");

    void IBasicConsumer.HandleModelShutdown(object model, ShutdownEventArgs reason) => throw new InvalidOperationException("Should never be called.");
}
#endif
