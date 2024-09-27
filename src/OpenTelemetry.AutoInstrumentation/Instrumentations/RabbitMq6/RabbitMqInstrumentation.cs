// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.DuckTypes;
using OpenTelemetry.Context.Propagation;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6;

internal static class RabbitMqInstrumentation
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.RabbitMq");

    public static Activity? StartReceive<TBasicResult, TModelBase>(TBasicResult result, DateTimeOffset startTime, TModelBase instance)
        where TBasicResult : IBasicGetResult
        where TModelBase : IModelBase
    {
        var connection = instance.Session?.Connection;
        return StartConsume(
            result.BasicProperties?.Headers,
            MessagingAttributes.Values.ReceiveOperationName,
            result.Exchange,
            result.RoutingKey,
            result.DeliveryTag,
            result.Body.Length,
            result.BasicProperties?.MessageId,
            result.BasicProperties?.CorrelationId,
            connection?.Endpoint?.HostName,
            connection?.Endpoint?.Port,
            connection?.RemoteEndPoint,
            startTime);
    }

    public static Activity? StartProcess<TBasicProperties, TBody>(TBasicProperties properties, string? exchange, string? routingKey, TBody body, ulong deliveryTag)
        where TBasicProperties : IBasicProperties
        where TBody : IBody
    {
        var messageBodyLength = body.Instance == null ? 0 : body.Length;
        IDictionary<string, object>? headers = null;
        string? messageId = null;
        string? correlationId = null;

        if (properties.Instance is not null)
        {
            headers = properties.Headers;
            messageId = properties.MessageId;
            correlationId = properties.CorrelationId;
        }

        return StartConsume(
            headers,
            MessagingAttributes.Values.DeliverOperationName,
            exchange,
            routingKey,
            deliveryTag,
            messageBodyLength,
            messageId,
            correlationId);
    }

    public static Activity? StartPublish<TBasicProperties, TModel>(TBasicProperties basicProperties, string? exchange, string? routingKey, int bodyLength, TModel instance)
    where TBasicProperties : IBasicProperties
    where TModel : IModelBase
    {
        var name = GetActivityName(routingKey, MessagingAttributes.Values.PublishOperationName);
        var activity = Source.StartActivity(name, ActivityKind.Producer);
        if (activity is not null && basicProperties.Instance is not null)
        {
            basicProperties.Headers ??= new Dictionary<string, object>();
            Propagators.DefaultTextMapPropagator.Inject(new PropagationContext(activity.Context, Baggage.Current), basicProperties.Headers, MessageHeaderValueSetter);
        }

        if (activity is { IsAllDataRequested: true })
        {
            var connection = instance.Session?.Connection;
            SetCommonTags(
                activity,
                exchange,
                routingKey,
                bodyLength,
                MessagingAttributes.Values.PublishOperationName,
                connection?.Endpoint?.HostName,
                connection?.Endpoint?.Port,
                connection?.RemoteEndPoint);
        }

        return activity;
    }

    private static string GetActivityName(string? routingKey, string operationType)
    {
        return string.IsNullOrEmpty(routingKey) ? operationType : $"{routingKey} {operationType}";
    }

    private static Activity? StartConsume(
        IDictionary<string, object>? headers,
        string consumeOperationName,
        string? exchange,
        string? routingKey,
        ulong deliveryTag,
        int bodyLength,
        string? messageId,
        string? correlationId,
        string? hostname = null,
        int? port = null,
        EndPoint? remoteEndpoint = null,
        DateTimeOffset startTime = default)
    {
        var propagatedContext = Propagators.DefaultTextMapPropagator.Extract(default, headers, MessageHeaderValueGetter);

        var activityLinks = propagatedContext.ActivityContext.IsValid()
            ? new[] { new ActivityLink(propagatedContext.ActivityContext) }
            : Array.Empty<ActivityLink>();

        var name = GetActivityName(routingKey, consumeOperationName);
        var activity = Source.StartActivity(
            name: name,
            kind: ActivityKind.Consumer,
            links: activityLinks,
            startTime: startTime);
        if (activity is { IsAllDataRequested: true })
        {
            SetConsumeTags(
                activity,
                exchange,
                routingKey,
                consumeOperationName,
                deliveryTag,
                bodyLength,
                messageId,
                correlationId,
                hostname,
                port,
                remoteEndpoint);
        }

        return activity;
    }

    private static void SetCommonTags(
        Activity activity,
        string? resultExchange,
        string? routingKey,
        int bodyLength,
        string operationName,
        string? hostName,
        int? port,
        EndPoint? remoteEndpoint)
    {
        var exchange = string.IsNullOrEmpty(resultExchange) ? MessagingAttributes.Values.RabbitMq.DefaultExchangeName : resultExchange;

        activity
            .SetTag(MessagingAttributes.Keys.MessagingSystem, MessagingAttributes.Values.RabbitMq.MessagingSystemName)
            .SetTag(MessagingAttributes.Keys.MessagingOperation, operationName)
            .SetTag(MessagingAttributes.Keys.DestinationName, exchange)
            .SetTag(MessagingAttributes.Keys.MessageBodySize, bodyLength);

        if (!string.IsNullOrEmpty(routingKey))
        {
            activity.SetTag(MessagingAttributes.Keys.RabbitMq.RoutingKey, routingKey);
        }

        var hostNameAndPortExist = !string.IsNullOrEmpty(hostName) && port != null;
        if (hostNameAndPortExist)
        {
            activity.SetTag(NetworkAttributes.Keys.ServerAddress, hostName);
            activity.SetTag(NetworkAttributes.Keys.ServerPort, port);
        }

        if (remoteEndpoint is IPEndPoint remoteIpEndpoint)
        {
            var remoteAddress = remoteIpEndpoint.Address.ToString();

            if (!hostNameAndPortExist)
            {
                activity.SetTag(NetworkAttributes.Keys.ServerAddress, remoteAddress);
                activity.SetTag(NetworkAttributes.Keys.ServerPort, remoteIpEndpoint.Port);
            }

            activity.SetTag(NetworkAttributes.Keys.NetworkPeerAddress, remoteAddress);
            activity.SetTag(NetworkAttributes.Keys.NetworkPeerPort, remoteIpEndpoint.Port);

            var networkType = remoteEndpoint.AddressFamily switch
            {
                AddressFamily.InterNetwork => NetworkAttributes.Values.IpV4NetworkType,
                AddressFamily.InterNetworkV6 => NetworkAttributes.Values.IpV6NetworkType,
                _ => null
            };

            if (networkType is not null)
            {
                activity.SetTag(NetworkAttributes.Keys.NetworkType, networkType);
            }
        }
    }

    private static void SetConsumeTags(
        Activity activity,
        string? exchange,
        string? routingKey,
        string operationName,
        ulong deliveryTag,
        int bodyLength,
        string? basicPropertiesMessageId,
        string? basicPropertiesCorrelationId,
        string? hostName,
        int? port,
        EndPoint? remoteEndpoint)
    {
        SetCommonTags(activity, exchange, routingKey, bodyLength, operationName, hostName, port, remoteEndpoint);
        if (!string.IsNullOrEmpty(basicPropertiesMessageId))
        {
            activity.SetTag(MessagingAttributes.Keys.MessageId, basicPropertiesMessageId);
        }

        if (!string.IsNullOrEmpty(basicPropertiesCorrelationId))
        {
            activity.SetTag(MessagingAttributes.Keys.ConversationId, basicPropertiesCorrelationId);
        }

        if (deliveryTag > 0)
        {
            activity.SetTag(MessagingAttributes.Keys.RabbitMq.DeliveryTag, deliveryTag);
        }
    }

    private static IEnumerable<string> MessageHeaderValueGetter(
        IDictionary<string, object>? headers,
        string key)
    {
        if (headers is not null && headers.TryGetValue(key, out var value) && value is byte[] bytes)
        {
            return new[] { Encoding.UTF8.GetString(bytes) };
        }

        return Enumerable.Empty<string>();
    }

    private static void MessageHeaderValueSetter(IDictionary<string, object> headers, string key, string val)
    {
        headers[key] = val;
    }
}
