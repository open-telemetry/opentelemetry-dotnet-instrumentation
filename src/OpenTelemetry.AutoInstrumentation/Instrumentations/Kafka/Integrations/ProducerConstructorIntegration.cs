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
        List<KeyValuePair<string, string>>? configList = null;

        if (producerBuilder.Config is not null)
        {
            configList = new List<KeyValuePair<string, string>>();
            foreach (var keyValuePair in producerBuilder.Config)
            {
                configList.Add(keyValuePair);
                if (string.Equals(
                        keyValuePair.Key,
                        BootstrapServersConfigKey,
                        StringComparison.OrdinalIgnoreCase))
                {
                    bootstrapServers = keyValuePair.Value;
                }
            }
        }

        if (!string.IsNullOrEmpty(bootstrapServers))
        {
            // Store the association between producer instance and bootstrap servers from configuration.
            // Will be used to schedule cluster ID fetch and populate "messaging.kafka.cluster.id" attribute.
            BootstrapServersCache.Add(instance!, bootstrapServers!);
            // Stash state so OnMethodEnd can schedule the fetch after the constructor completes
            // and the producer's rdkafka handle is fully initialized.
            return new CallTargetState(null, new KafkaConstructorFetchState(bootstrapServers!, configList));
        }

        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (exception is not null)
        {
            BootstrapServersCache.Remove(instance!);
        }
        else if (state.State is KafkaConstructorFetchState fetchState)
        {
            // Constructor succeeded — the producer's rdkafka handle is now ready.
            // Pass the instance so the fetch can use DependentAdminClientBuilder
            // and avoid allocating new rdkafka handles that would shift client-id counters.
            KafkaClusterIdCache.ScheduleFetch(fetchState.BootstrapServers, fetchState.Config, instance);
        }

        return CallTargetReturn.GetDefault();
    }
}
