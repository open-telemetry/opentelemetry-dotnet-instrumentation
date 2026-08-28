// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;

internal static class XmsInstrumentation
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.Xms", AutoInstrumentationVersion.Version);

    public static Activity? StartProducerActivity(IXmsMessage message, object? destinationCandidate, object? producerInstance)
    {
        var destination = ResolveDestinationName(destinationCandidate)
                           ?? ResolveDestinationName(TryDuckCastOrNull<IXmsMessageProducer>(producerInstance)?.Destination);

        var name = destination is null
            ? MessagingAttributes.Values.PublishOperationName
            : $"{destination} {MessagingAttributes.Values.PublishOperationName}";

        var activity = Source.StartActivity(name, ActivityKind.Producer);

        if (activity is { IsAllDataRequested: true })
        {
            SetCommonTags(activity, message.JMSMessageID, message.JMSCorrelationID, destination, MessagingAttributes.Values.PublishOperationName);
        }

        if (activity is not null)
        {
            try
            {
                Propagators.DefaultTextMapPropagator.Inject(
                    new PropagationContext(activity.Context, Baggage.Current),
                    message,
                    XmsMessageSetter.Setter);
            }
            catch
            {
                // Instrumentation must never throw or break the customer application.
            }
        }

        return activity;
    }

    public static Activity? StartConsumerActivity(IXmsMessage? message, object? consumerInstance, string operationName)
    {
        if (message is null)
        {
            return null;
        }

        var destination = ResolveDestinationName(message.JMSDestination) ?? ResolveDestinationName(consumerInstance);

        return StartConsumerActivityCore(message, XmsPropertyGetter.GetValues, message.JMSMessageID, message.JMSCorrelationID, destination, operationName);
    }

    public static Activity? StartConsumerActivity(IXmsProviderMessage? message, object? consumerInstance, string operationName)
    {
        if (message is null)
        {
            return null;
        }

        var destination = NormalizeProviderDestination(message.JMSDestinationAsString) ?? ResolveDestinationName(consumerInstance);

        return StartConsumerActivityCore(message, XmsPropertyGetter.GetValues, message.JMSMessageID, message.JMSCorrelationID, destination, operationName);
    }

    public static void EndActivity(Activity activity, Exception? exception)
    {
        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity.Dispose();
    }

    /// <summary>
    /// JMS assigns JMSMessageID (and, when the application hasn't already set one, JMSCorrelationID)
    /// during Send, so both are unavailable at OnMethodBegin and must be read after the underlying
    /// Send call returns.
    /// </summary>
    /// <param name="activity">The producer activity started in OnMethodBegin.</param>
    /// <param name="messageState">The duck-typed message instance stashed in CallTargetState.State.</param>
    public static void EnrichProducerActivityOnEnd(Activity activity, object? messageState)
    {
        if (activity is not { IsAllDataRequested: true })
        {
            return;
        }

        if (messageState is not IXmsMessage message)
        {
            return;
        }

        try
        {
            var messageId = message.JMSMessageID;
            if (!string.IsNullOrEmpty(messageId))
            {
                activity.SetTag(MessagingAttributes.Keys.MessageId, messageId);
            }

            var correlationId = message.JMSCorrelationID;
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag(MessagingAttributes.Keys.ConversationId, correlationId);
            }
        }
        catch
        {
            // Instrumentation must never throw or break the customer application.
        }
    }

    private static Activity? StartConsumerActivityCore<TCarrier>(
        TCarrier carrier,
        Func<TCarrier, string, IEnumerable<string>?> getter,
        string? messageId,
        string? correlationId,
        string? destination,
        string operationName)
    {
        var extracted = default(PropagationContext);
        try
        {
            extracted = Propagators.DefaultTextMapPropagator.Extract(default, carrier, getter);
        }
        catch
        {
            // Fall back to a default propagation context on any extraction failure.
        }

        var name = destination is null ? operationName : $"{destination} {operationName}";

        Activity? activity;
        if (extracted.ActivityContext != default)
        {
            activity = Source.StartActivity(name, ActivityKind.Consumer, extracted.ActivityContext);

            if (extracted.Baggage.Count > 0)
            {
                Baggage.Current = extracted.Baggage;
            }
        }
        else
        {
            activity = Source.StartActivity(name, ActivityKind.Consumer);
        }

        if (activity is { IsAllDataRequested: true })
        {
            SetCommonTags(activity, messageId, correlationId, destination, operationName);
        }

        return activity;
    }

    private static string? NormalizeProviderDestination(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        var value = raw;

        var schemeIndex = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex >= 0)
        {
            value = value.Substring(schemeIndex + 3).TrimStart('/');
        }

        var queryIndex = value.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            value = value.Substring(0, queryIndex);
        }

        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string? ResolveDestinationName(object? candidate)
    {
        if (candidate is null)
        {
            return null;
        }

        var destination = TryDuckCastOrNull<IXmsDestination>(candidate);
        if (!string.IsNullOrEmpty(destination?.Name))
        {
            return destination!.Name;
        }

        return null;
    }

    private static T? TryDuckCastOrNull<T>(object? candidate)
    {
        if (candidate is null)
        {
            return default;
        }

        try
        {
            return candidate.TryDuckCast<T>(out var value) ? value : default;
        }
        catch
        {
            return default;
        }
    }

    private static void SetCommonTags(Activity activity, string? messageId, string? correlationId, string? destination, string operationName)
    {
        activity.SetTag(MessagingAttributes.Keys.MessagingSystem, MessagingAttributes.Values.IbmMqMessagingSystemName);
        activity.SetTag(MessagingAttributes.Keys.MessagingOperation, operationName);

        if (destination is not null)
        {
            activity.SetTag(MessagingAttributes.Keys.DestinationName, destination);
        }

        if (!string.IsNullOrEmpty(messageId))
        {
            activity.SetTag(MessagingAttributes.Keys.MessageId, messageId);
        }

        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag(MessagingAttributes.Keys.ConversationId, correlationId);
        }
    }
}
