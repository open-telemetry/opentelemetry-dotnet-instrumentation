// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.Integrations;

/// <summary>
/// IBM XMS async delivery instrumentation: IBM.XMS.Client.Impl.XmsProviderMessageListener.OnMessage(IBM.XMS.Provider.ProviderMessage)
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.XmsAssemblyName,
    typeName: IntegrationConstants.MessageListenerTypeName,
    methodName: IntegrationConstants.OnMessageMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [IntegrationConstants.ProviderMessageTypeName],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class MessageListenerOnMessageIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TMessage>(TTarget instance, TMessage message)
        where TMessage : IXmsProviderMessage, IDuckType
    {
        if (message.Instance is null)
        {
            return CallTargetState.GetDefault();
        }

        return new CallTargetState(XmsInstrumentation.StartConsumerActivity(message, instance, MessagingAttributes.Values.DeliverOperationName), null);
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (state.Activity is null)
        {
            return CallTargetReturn.GetDefault();
        }

        XmsInstrumentation.EndActivity(state.Activity, exception);

        return CallTargetReturn.GetDefault();
    }
}
