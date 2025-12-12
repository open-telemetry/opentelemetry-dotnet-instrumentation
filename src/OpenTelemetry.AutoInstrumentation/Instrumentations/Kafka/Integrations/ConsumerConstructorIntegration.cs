// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka consumer ctor instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ConsumerTypeName,
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [IntegrationConstants.ConsumerBuilderTypeName],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerConstructorIntegration
{
    private const string ConsumerGroupIdConfigKey = "group.id";

    internal static CallTargetState OnMethodBegin<TTarget, TConsumerBuilder>(TTarget instance, TConsumerBuilder consumerBuilder)
    where TConsumerBuilder : IConsumerBuilder, IDuckType
    {
        // duck type created for consumer builder is a struct
        if (consumerBuilder.Instance is null)
        {
            // invalid parameters, exit early
            return CallTargetState.GetDefault();
        }

        string? consumerGroupId = null;

        if (consumerBuilder.Config is not null)
        {
            foreach (var keyValuePair in consumerBuilder.Config)
            {
                if (string.Equals(
                        keyValuePair.Key,
                        ConsumerGroupIdConfigKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    consumerGroupId = keyValuePair.Value;
                    break;
                }
            }
        }

        // https://github.com/confluentinc/confluent-kafka-dotnet/wiki/Consumer#misc-points states GroupId is required
        if (consumerGroupId is not null)
        {
            // Store the association between consumer instance and "group.id" from configuration,
            // will be used to populate "messaging.kafka.consumer.group" attribute value
            ConsumerCache.Add(instance!, consumerGroupId);
        }

        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is not null)
        {
            ConsumerCache.Remove(instance!);
        }

        return CallTargetReturn.GetDefault();
    }
}
