// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.Integrations;

/// <summary>
/// RabbitMq AsyncDefaultBasicConsumer.HandleBasicDeliver integration.
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.RabbitMqAssemblyName,
    typeName: IntegrationConstants.AsyncDefaultBasicConsumerTypeName,
    methodName: IntegrationConstants.HandleBasicDeliverMethodName,
    returnTypeName: ClrNames.Task,
    parameterTypeNames: new[] { ClrNames.String, ClrNames.UInt64, ClrNames.Bool, ClrNames.String, ClrNames.String, IntegrationConstants.BasicPropertiesInterfaceTypeName, $"System.ReadOnlyMemory`1[{ClrNames.Byte}]" },
    minimumVersion: IntegrationConstants.MinSupportedVersion,
    maximumVersion: IntegrationConstants.MaxSupportedVersion,
    integrationName: IntegrationConstants.RabbitMqByteCodeIntegrationName,
    type: InstrumentationType.Trace,
    integrationKind: IntegrationKind.Derived)]
public static class AsyncDefaultBasicConsumerIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TBasicProperties, TBody>(TTarget instance, string? consumerTag, ulong deliveryTag, bool redelivered, string? exchange, string? routingKey, TBasicProperties properties, TBody body)
        where TBasicProperties : IBasicProperties
        where TBody : IBody
    {
        var activity = RabbitMqInstrumentation.StartProcess(properties, exchange, routingKey, body, deliveryTag);
        return new CallTargetState(activity, null);
    }

    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return returnValue;
        }

        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity.Stop();

        return returnValue;
    }
}
