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
    private const string BootstrapServersConfigKey = "bootstrap.servers";

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
        string? bootstrapServers = null;
        List<KeyValuePair<string, string>>? configList = null;

        if (consumerBuilder.Config is not null)
        {
            configList = new List<KeyValuePair<string, string>>();
            foreach (var keyValuePair in consumerBuilder.Config)
            {
                configList.Add(keyValuePair);
                if (string.Equals(
                        keyValuePair.Key,
                        ConsumerGroupIdConfigKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    consumerGroupId = keyValuePair.Value;
                }
                else if (string.Equals(
                        keyValuePair.Key,
                        BootstrapServersConfigKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    bootstrapServers = keyValuePair.Value;
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

        if (!string.IsNullOrEmpty(bootstrapServers))
        {
            // Store the association between consumer instance and bootstrap servers from configuration.
            // Will be used to populate "messaging.cluster.id" attribute value.
            BootstrapServersCache.Add(instance!, bootstrapServers!);
            KafkaClusterIdCache.ScheduleFetch(bootstrapServers!, configList);
        }

        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is not null)
        {
            ConsumerCache.Remove(instance!);
            BootstrapServersCache.Remove(instance!);
        }

        return CallTargetReturn.GetDefault();
    }
}
