// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.Integrations;

/// <summary>
/// IBM XMS message consumer Receive(long) instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.XmsAssemblyName,
    typeName: IntegrationConstants.MessageConsumerTypeName,
    methodName: IntegrationConstants.ReceiveMethodName,
    returnTypeName: IntegrationConstants.MessageInterfaceTypeName,
    parameterTypeNames: [ClrNames.Int64],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerReceiveWithTimeoutIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, long timeout)
    {
        return new CallTargetState(null, instance);
    }

    internal static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception? exception, in CallTargetState state)
    {
        if (response is null)
        {
            return new CallTargetReturn<TResponse>(response);
        }

        var duckMessage = response.DuckCast<IXmsMessage>();
        if (duckMessage is null)
        {
            return new CallTargetReturn<TResponse>(response);
        }

        var activity = XmsInstrumentation.StartConsumerActivity(duckMessage, instance, MessagingAttributes.Values.ReceiveOperationName);
        if (activity is not null)
        {
            XmsInstrumentation.EndActivity(activity, exception);
        }

        return new CallTargetReturn<TResponse>(response);
    }
}
