// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Resources;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.RabbitMq6.Integrations;

/// <summary>
/// RabbitMq ModelBase.BasicGet integration.
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.RabbitMqAssemblyName,
    typeName: IntegrationConstants.ModelBaseTypeName,
    methodName: IntegrationConstants.BasicGetMethodName,
    returnTypeName: IntegrationConstants.BasicGetResultTypeName,
    parameterTypeNames: new[] { ClrNames.String, ClrNames.Bool },
    minimumVersion: IntegrationConstants.MinSupportedVersion,
    maximumVersion: IntegrationConstants.MaxSupportedVersion,
    integrationName: IntegrationConstants.RabbitMqByteCodeIntegrationName,
    type: InstrumentationType.Trace)]
public static class ModelBaseBasicGetIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, string queue, bool autoAck)
    where TTarget : IModelBase
    {
        var activity = RabbitMqInstrumentation.StartReceive(instance);
        return new CallTargetState(activity, null);
    }

    internal static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception? exception, in CallTargetState state)
    where TResponse : IBasicGetResult
    {
        /*        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (response.Instance is not null)
                {
                    using var activity = RabbitMqInstrumentation.StartReceive(response, state.StartTime!.Value, instance);
                    if (exception is not null)
                    {
                        activity.SetException(exception);
                    }
                }

                return new CallTargetReturn<TResponse>(response);*/

        var activity = state.Activity;
        if (activity is null)
        {
            return new CallTargetReturn<TResponse>(response);
        }

        RabbitMqInstrumentation.EndReceive(activity, response);

        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity.Stop();
        return new CallTargetReturn<TResponse>(response);
    }
}
