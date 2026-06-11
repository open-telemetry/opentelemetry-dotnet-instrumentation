// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka producer ctor instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ProducerTypeName,
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [IntegrationConstants.ProducerBuilderTypeName],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ProducerConstructorIntegration
{
    private const string BootstrapServersConfigKey = "bootstrap.servers";

    internal static CallTargetState OnMethodBegin<TTarget, TProducerBuilder>(TTarget instance, TProducerBuilder producerBuilder)
    where TProducerBuilder : IProducerBuilder, IDuckType
    {
        // Duck type created for producer builder is a struct
        if (producerBuilder.Instance is null)
        {
            // invalid parameters, exit early
            return CallTargetState.GetDefault();
        }

        string? bootstrapServers = null;

        if (producerBuilder.Config is not null)
        {
            foreach (var keyValuePair in producerBuilder.Config)
            {
                if (string.Equals(
                        keyValuePair.Key,
                        BootstrapServersConfigKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    bootstrapServers = keyValuePair.Value;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(bootstrapServers))
        {
            // Store the association between producer instance and bootstrap servers from configuration.
            // Will be used to schedule cluster ID fetch and populate "messaging.kafka.cluster.id" attribute.
            BootstrapServersCache.Add(instance!, bootstrapServers!);
            KafkaClusterIdCache.ScheduleFetch(bootstrapServers!);
        }

        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is not null)
        {
            BootstrapServersCache.Remove(instance!);
        }

        return CallTargetReturn.GetDefault();
    }
}
