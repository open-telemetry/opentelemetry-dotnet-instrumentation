// <copyright file="ConsumerUnsubscribeIntegration.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka consumer Unsubscribe instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ConsumerTypeName,
    methodName: IntegrationConstants.UnsubscribeMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { IntegrationConstants.ConsumerBuilderTypeName },
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerUnsubscribeIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        // Unsubscribing the consumer marks the end of processing a message
        KafkaCommon.StopCurrentConsumerActivity();
        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        return CallTargetReturn.GetDefault();
    }
}
