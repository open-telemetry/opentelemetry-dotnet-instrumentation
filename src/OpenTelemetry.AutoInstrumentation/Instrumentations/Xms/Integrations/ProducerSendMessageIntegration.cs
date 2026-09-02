// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.Integrations;

/// <summary>
/// IBM XMS message producer Send(IMessage) instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.XmsAssemblyName,
    typeName: IntegrationConstants.MessageProducerTypeName,
    methodName: IntegrationConstants.SendMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [IntegrationConstants.MessageInterfaceTypeName],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ProducerSendMessageIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TMessage>(TTarget instance, TMessage message)
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
