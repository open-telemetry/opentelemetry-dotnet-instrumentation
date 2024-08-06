// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.DuckTypes;

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/api/BasicGetResult.cs
internal interface IBasicGetResult : IDuckType
{
    public string? Exchange { get; set; }

    public string? RoutingKey { get; set; }

    public IBasicProperties? BasicProperties { get; set; }

    public ulong DeliveryTag { get; set; }

    public IBody Body { get; set; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/api/IBasicProperties.cs
internal interface IBasicProperties : IDuckType
{
    IDictionary<string, object>? Headers { get; set; }

    string? CorrelationId { get; set; }

    string? MessageId { get; set; }
}
