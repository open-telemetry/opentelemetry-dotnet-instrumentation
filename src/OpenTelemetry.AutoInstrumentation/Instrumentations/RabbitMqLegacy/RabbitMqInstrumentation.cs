// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.DuckTypes;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy;

internal static class RabbitMqInstrumentation
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.RabbitMq", AutoInstrumentationVersion.Version);

    public static Activity? StartReceive<TModelBase>(TModelBase instance)
        where TModelBase : IModelBase
    {
        var connection = instance.Session?.Connection;
        var activity = Source.StartActivity(name: string.Empty, kind: ActivityKind.Consumer);

        SetNetworkTags(
            activity!,
            connection?.Endpoint?.HostName,
            connection?.Endpoint?.Port,
            connection?.RemoteEndPoint);

        return activity;
    }

    public static void EndReceive<TBasicResult>(Activity activity, TBasicResult result)
        where TBasicResult : IBasicGetResult
    {
        activity.DisplayName = GetActivityName(result.RoutingKey, MessagingAttributes.Values.ReceiveOperationName);

        var activityLinks = GetActivityLinks(result.BasicProperties?.Headers);

        foreach (var link in activityLinks)
        {
            activity.AddLink(link);
        }

        activity.SetCommonTags(result.Exchange, result.RoutingKey, result.Body.Length, MessagingAttributes.Values.ReceiveOperationName);

        activity.SetMessagingTags(result.BasicProperties?.MessageId, result.BasicProperties?.CorrelationId, result.DeliveryTag);
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

        var activityLinks = GetActivityLinks(headers);
        var name = GetActivityName(routingKey, MessagingAttributes.Values.DeliverOperationName);

        var activity = Source.StartActivity(
            name: name,
            kind: ActivityKind.Consumer,
            links: activityLinks);

        if (activity is { IsAllDataRequested: true })
        {
            activity.SetCommonTags(exchange, routingKey, messageBodyLength, MessagingAttributes.Values.DeliverOperationName);

            activity.SetMessagingTags(messageId, correlationId, deliveryTag);
        }

        return activity;
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

            SetNetworkTags(
                activity,
                connection?.Endpoint?.HostName,
                connection?.Endpoint?.Port,
                connection?.RemoteEndPoint);

            SetCommonTags(
                activity,
                exchange,
                routingKey,
                bodyLength,
                MessagingAttributes.Values.PublishOperationName);
        }

        return activity;
    }

    private static string GetActivityName(string? routingKey, string operationType)
    {
        return string.IsNullOrEmpty(routingKey) ? operationType : $"{routingKey} {operationType}";
    }

    private static ActivityLink[] GetActivityLinks(IDictionary<string, object>? headers)
    {
        var propagatedContext = Propagators.DefaultTextMapPropagator.Extract(default, headers, MessageHeaderValueGetter);

        return propagatedContext.ActivityContext.IsValid() ? [new ActivityLink(propagatedContext.ActivityContext)] : [];
    }

    private static void SetMessagingTags(this Activity activity, string? basicPropertiesMessageId, string? basicPropertiesCorrelationId, ulong deliveryTag)
    {
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

    private static void SetNetworkTags(this Activity activity, string? hostName, int? port, EndPoint? remoteEndpoint)
    {
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

    private static void SetCommonTags(
        this Activity activity,
        string? resultExchange,
        string? routingKey,
        int bodyLength,
        string operationName)
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
    }

    private static IEnumerable<string> MessageHeaderValueGetter(
        IDictionary<string, object>? headers,
        string key)
    {
        if (headers is not null && headers.TryGetValue(key, out var value) && value is byte[] bytes)
        {
            return [Encoding.UTF8.GetString(bytes)];
        }

        return [];
    }

    private static void MessageHeaderValueSetter(IDictionary<string, object> headers, string key, string val)
    {
        headers[key] = val;
    }
}
