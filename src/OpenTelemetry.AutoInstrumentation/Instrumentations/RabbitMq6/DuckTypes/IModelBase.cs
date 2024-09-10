// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.DuckTypes;

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/impl/ModelBase.cs
internal interface IModelBase
{
    ISession? Session { get; }
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
}

// wraps https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/a50334f2acb09fd16dc9cbd20ad1c6dd093d1d64/projects/RabbitMQ.Client/client/api/AmqpTcpEndpoint.cs
internal interface IAmqpTcpEndpoint
{
    string HostName { get; set; }

    int Port { get; set; }
}
