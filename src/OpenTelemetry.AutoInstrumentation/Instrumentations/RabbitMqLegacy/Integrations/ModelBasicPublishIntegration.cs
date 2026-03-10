// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMqLegacy.Integrations;

/// <summary>
/// Model BasicPublish integration.
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.RabbitMqAssemblyName,
    typeName: IntegrationConstants.ModelGeneratedTypeName,
    methodName: IntegrationConstants.BasicPublishMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, ClrNames.String, ClrNames.Bool, IntegrationConstants.BasicPropertiesInterfaceTypeName, $"{ClrNames.Byte}[]"],
    minimumVersion: IntegrationConstants.Min5SupportedVersion,
    maximumVersion: IntegrationConstants.Max5SupportedVersion,
    integrationName: IntegrationConstants.RabbitMqByteCodeIntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: IntegrationConstants.RabbitMqAssemblyName,
    typeName: IntegrationConstants.ModelGeneratedTypeName,
    methodName: IntegrationConstants.BasicPublishMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, ClrNames.String, ClrNames.Bool, IntegrationConstants.BasicPropertiesInterfaceTypeName, $"System.ReadOnlyMemory`1[{ClrNames.Byte}]"],
    minimumVersion: IntegrationConstants.Min6SupportedVersion,
    maximumVersion: IntegrationConstants.Max6SupportedVersion,
    integrationName: IntegrationConstants.RabbitMqByteCodeIntegrationName,
    type: InstrumentationType.Trace)]
public static class ModelBasicPublishIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TBasicProperties, TBody>(
        TTarget instance, string? exchange, string? routingKey, bool mandatory, TBasicProperties basicProperties, TBody body)
    where TBasicProperties : IBasicProperties
    where TBody : IBody
    where TTarget : IModelBase
    {
        var messageBodyLength = body.Instance == null ? 0 : body.Length;
        var activity = RabbitMqInstrumentation.StartPublish(basicProperties, exchange, routingKey, messageBodyLength, instance);
        if (activity is not null)
        {
            return new CallTargetState(activity);
        }

        return CallTargetState.GetDefault();
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
