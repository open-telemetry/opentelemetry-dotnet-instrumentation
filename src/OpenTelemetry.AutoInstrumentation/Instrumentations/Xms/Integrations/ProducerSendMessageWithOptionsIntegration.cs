// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.Integrations;

/// <summary>
/// IBM XMS message producer Send(IMessage, DeliveryMode, int, long) instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.XmsAssemblyName,
    typeName: IntegrationConstants.MessageProducerTypeName,
    methodName: IntegrationConstants.SendMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [IntegrationConstants.MessageInterfaceTypeName, IntegrationConstants.DeliveryModeTypeName, ClrNames.Int32, ClrNames.Int64],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ProducerSendMessageWithOptionsIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TMessage, TDeliveryMode>(TTarget instance, TMessage message, TDeliveryMode deliveryMode, int priority, long timeToLive)
        where TMessage : IXmsMessage, IDuckType
    {
        if (message.Instance is null)
        {
            return CallTargetState.GetDefault();
        }

        return new CallTargetState(XmsInstrumentation.StartProducerActivity(message, null, instance), message);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (state.Activity is null)
        {
            return CallTargetReturn.GetDefault();
        }

        XmsInstrumentation.EnrichProducerActivityOnEnd(state.Activity, state.State);
        XmsInstrumentation.EndActivity(state.Activity, exception);

        return CallTargetReturn.GetDefault();
    }
}
