// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.Integrations;

/// <summary>
/// RabbitMq DefaultBasicConsumer.HandleBasicDeliver integration.
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.RabbitMqAssemblyName,
    typeName: IntegrationConstants.DefaultBasicConsumerTypeName,
    methodName: IntegrationConstants.HandleBasicDeliverMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, IntegrationConstants.BasicPropertiesInterfaceTypeName, $"{ClrNames.Byte}[]"],
    minimumVersion: IntegrationConstants.Min5SupportedVersion,
    maximumVersion: IntegrationConstants.Max5SupportedVersion,
    integrationName: IntegrationConstants.RabbitMqByteCodeIntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
[InstrumentMethod(
    assemblyName: IntegrationConstants.RabbitMqAssemblyName,
    typeName: IntegrationConstants.DefaultBasicConsumerTypeName,
    methodName: IntegrationConstants.HandleBasicDeliverMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, IntegrationConstants.BasicPropertiesInterfaceTypeName, $"System.ReadOnlyMemory`1[{ClrNames.Byte}]"],
    minimumVersion: IntegrationConstants.Min6SupportedVersion,
    maximumVersion: IntegrationConstants.Max6SupportedVersion,
    integrationName: IntegrationConstants.RabbitMqByteCodeIntegrationName,
    type: InstrumentationType.Trace,
    kind: IntegrationKind.Derived)]
public static class DefaultBasicConsumerIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TBasicProperties, TBody>(TTarget instance, string? consumerTag, ulong deliveryTag, bool redelivered, string? exchange, string? routingKey, TBasicProperties properties, TBody body)
        where TBasicProperties : IBasicProperties
        where TBody : IBody
    {
        var activity = RabbitMqInstrumentation.StartProcess(properties, exchange, routingKey, body, deliveryTag);
        return new CallTargetState(activity, null);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return CallTargetReturn.GetDefault();
        }

        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity.Stop();

        return CallTargetReturn.GetDefault();
    }
}
