// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Net;
using OpenTelemetry.AutoInstrumentation.DuckTyping;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.DuckTypes;

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/impl/ModelBase.cs
internal interface IModelBase
{
    ISession? Session { get; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/DefaultBasicConsumer.cs
internal interface IBasicConsumer
{
    // Deliberately untyped: when automatic connection recovery is enabled (the client default),
    // this is an AutorecoveringModel, not a ModelBase - duck-casting it to IModelBase directly
    // here would throw eagerly, since AutorecoveringModel has no Session of its own. Callers must
    // resolve it defensively (see RabbitMqInstrumentation.GetConnectionFromConsumerModel).
    object? Model { get; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/impl/AutorecoveringModel.cs
// Used instead of a plain ModelBase-derived channel whenever automatic connection recovery is
// enabled (the client default). It does not derive from ModelBase itself, so it has no Session -
// the real channel is reachable through its public Delegate property.
internal interface IAutorecoveringModel
{
    object? Delegate { get; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/impl/Session.cs
internal interface ISession
{
    IConnection? Connection { get; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/impl/Connection.cs
internal interface IConnection
{
    IAmqpTcpEndpoint? Endpoint { get; }

    EndPoint? RemoteEndPoint { get; }

    // Populated from the broker handshake (connection.start's server-properties table).
    // Values are the raw AMQP field values, so string entries (like "cluster_name")
    // arrive as UTF8 byte[], not string.
    IDictionary? ServerProperties { get; }

    // Connection itself has no vhost accessor in this era of the client - it's only
    // held on the private factory reference used to open the connection.
    [DuckField(Name = "_factory")]
    IConnectionFactory? Factory { get; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/api/ConnectionFactory.cs
internal interface IConnectionFactory
{
    string? VirtualHost { get; }
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/api/AmqpTcpEndpoint.cs
internal interface IAmqpTcpEndpoint
{
    string HostName { get; set; }

    int Port { get; set; }
}
