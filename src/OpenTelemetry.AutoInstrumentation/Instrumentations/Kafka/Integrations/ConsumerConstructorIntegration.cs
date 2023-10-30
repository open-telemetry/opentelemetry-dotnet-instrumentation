// <copyright file="ConsumerConstructorIntegration.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka consumer ctor instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ConsumerTypeName,
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { IntegrationConstants.ConsumerBuilderTypeName },
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerConstructorIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TConsumerBuilder>(TTarget instance, TConsumerBuilder consumerBuilder)
    where TConsumerBuilder : IConsumerBuilder
    {
        string? consumerGroupId = null;

        foreach (var keyValuePair in consumerBuilder.Config)
        {
            if (string.Equals(keyValuePair.Key, KafkaCommon.ConsumerGroupIdConfigKey, StringComparison.OrdinalIgnoreCase))
            {
                consumerGroupId = keyValuePair.Value;
                break;
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
