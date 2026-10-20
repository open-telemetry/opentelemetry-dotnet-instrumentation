// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if !RABBITMQ_7_0_0_OR_GREATER
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TestApplication.RabbitMq;

// This custom consumer exercises instrumentation of an implicit implementation
// of RabbitMQ.Client.IBasicConsumer.HandleBasicDeliver.
internal sealed class SyncImplicitImplementationConsumer : IBasicConsumer
{
    private readonly DefaultBasicConsumer _consumer;

    public SyncImplicitImplementationConsumer(IModel model)
    {
        _consumer = new DefaultBasicConsumer(model);
        Model = model;
    }

    public event EventHandler<BasicDeliverEventArgs>? Received;

    public event EventHandler<ConsumerEventArgs>? ConsumerCancelled
    {
        add => _consumer.ConsumerCancelled += value;
        remove => _consumer.ConsumerCancelled -= value;
    }

    public IModel Model { get; }

    public void HandleBasicCancel(string consumerTag) => _consumer.HandleBasicCancel(consumerTag);

    public void HandleBasicCancelOk(string consumerTag) => _consumer.HandleBasicCancelOk(consumerTag);

    public void HandleBasicConsumeOk(string consumerTag) => _consumer.HandleBasicConsumeOk(consumerTag);

    public void HandleBasicDeliver(
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
        _consumer.HandleBasicDeliver(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body);
        Received?.Invoke(this, new BasicDeliverEventArgs(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body));
    }

    public void HandleModelShutdown(object model, ShutdownEventArgs reason) => _consumer.HandleModelShutdown(model, reason);
}
#endif
