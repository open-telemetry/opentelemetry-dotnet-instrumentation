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
    private static readonly Lazy<bool> ExperimentalSpanAttributesEnabled = new(() => Instrumentation.TracerSettings.Value.InstrumentationOptions.XmsExperimentalSpanAttributes);

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

        SetQueueManagerId(activity, producerInstance);

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

        var activity = StartConsumerActivityCore(message, XmsPropertyGetter.GetValues, message.JMSMessageID, message.JMSCorrelationID, destination, operationName);

        // consumerInstance is the IMessageConsumer itself here, which implements IPropertyContext directly.
        SetQueueManagerId(activity, consumerInstance);

        return activity;
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

        activity.Stop();
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

    /// <summary>
    /// Reads the IBM MQ queue manager identifier off the live connection property map and stamps it
    /// onto the span as <c>messaging.ibmmq.queue_manager.id</c>, gated behind the experimental span
    /// attributes flag. The value is never cached: IBM refreshes the resolved connection properties
    /// after an automatic client reconnect, which can land on a different queue manager, so this must
    /// be re-read per operation.
    /// </summary>
    /// <param name="activity">The span to enrich, or null/non-recording, in which case this is a no-op.</param>
    /// <param name="propertyContextCandidate">
    /// An instance expected to duck-cast to <see cref="IXmsPropertyContext"/> (an XMS producer,
    /// consumer, session, or connection instance). Never throws on a mismatch.
    /// </param>
    public static void SetQueueManagerId(Activity? activity, object? propertyContextCandidate)
    {
        if (!ExperimentalSpanAttributesEnabled.Value || activity is not { IsAllDataRequested: true } || propertyContextCandidate is null)
        {
            return;
        }

        try
        {
            if (!propertyContextCandidate.TryDuckCast<IXmsPropertyContext>(out var propertyContext))
            {
                return;
            }

            if (!propertyContext.PropertyExists(IntegrationConstants.ResolvedQueueManagerIdPropertyName))
            {
                return;
            }

            var queueManagerId = propertyContext.GetStringProperty(IntegrationConstants.ResolvedQueueManagerIdPropertyName)?.Trim();
            if (!string.IsNullOrEmpty(queueManagerId))
            {
                activity.SetTag(MessagingAttributes.Keys.IbmMq.QueueManagerId, queueManagerId);
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

        if (extracted.Baggage.Count > 0)
        {
            Baggage.Current = extracted.Baggage;
        }

        var name = destination is null ? operationName : $"{destination} {operationName}";
        var activityLinks = GetActivityLinks(extracted.ActivityContext);

        var activity = Source.StartActivity(name: name, kind: ActivityKind.Consumer, links: activityLinks);

        if (activity is { IsAllDataRequested: true })
        {
            SetCommonTags(activity, messageId, correlationId, destination, operationName);
        }

        return activity;
    }

    private static ActivityLink[] GetActivityLinks(ActivityContext activityContext)
    {
        return activityContext.IsValid() ? [new ActivityLink(activityContext)] : [];
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
