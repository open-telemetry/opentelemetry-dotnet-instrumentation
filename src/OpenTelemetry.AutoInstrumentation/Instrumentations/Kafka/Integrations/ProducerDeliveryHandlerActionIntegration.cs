// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka sync produce instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ProducerDeliveryHandlerShimTypeName,
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [ClrNames.String, "!0", "!1", IntegrationConstants.ActionOfDeliveryReportTypeName],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ProducerDeliveryHandlerActionIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TKey, TValue, TActionOfDeliveryReport>(
        TTarget instance,
        string topic,
        TKey key,
        TValue value,
        TActionOfDeliveryReport? handler)
    {
        // If handler is not set,
        // activity will be stopped at the end
        // of Produce method, there is nothing to do
        if (handler == null)
        {
            return CallTargetState.GetDefault();
        }

        try
        {
            var currentActivity = Activity.Current;
            if (currentActivity is not null)
            {
                var activityCompletingHandler = WrapperCache<TActionOfDeliveryReport>.Create(handler, currentActivity);

                // Store the action to set activity handler as state,
                // run at the end of the ctor.
                Action<IDeliveryHandlerActionShim> setActivityCompletingHandler = shim => shim.Handler = activityCompletingHandler!;
                return new CallTargetState(currentActivity, setActivityCompletingHandler);
            }

            return CallTargetState.GetDefault();
        }
        catch (Exception)
        {
            return CallTargetState.GetDefault();
        }
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        if (state.State is Action<IDeliveryHandlerActionShim> action && instance.TryDuckCast<IDeliveryHandlerActionShim>(out var inst))
        {
            try
            {
                action.Invoke(inst);
            }
            catch (Exception)
            {
                state.Activity?.Stop();
            }
        }

        return CallTargetReturn.GetDefault();
    }

    internal static Action<TDeliveryReport> WrapWithActivityCompletion<TDeliveryReport>(Action<TDeliveryReport> initialCallback, Activity activity)
    {
        return report =>
        {
            if (report.TryDuckCast<IDeliveryReport>(out var deliveryReport))
            {
                if (deliveryReport.Error is { IsError: true })
                {
#pragma warning disable CA2201 // We need to create a generic exception here to capture error information
                    activity.SetException(new Exception(deliveryReport.Error.ToString()));
#pragma warning restore CA2201 // We need to create a generic exception here to capture error information
                }

                KafkaInstrumentation.SetDeliveryResults(activity, deliveryReport);
            }

            try
            {
                initialCallback.Invoke(report);
            }
            finally
            {
                activity.Stop();
            }
        };
    }

    private static class WrapperCache<TActionOfDeliveryReport>
    {
        private static readonly CreateWrapperDelegate Delegate = Initialize();

        private delegate TActionOfDeliveryReport CreateWrapperDelegate(TActionOfDeliveryReport action, Activity activity);

        public static TActionOfDeliveryReport Create(TActionOfDeliveryReport action, Activity activity)
        {
            return Delegate(action, activity);
        }

        private static CreateWrapperDelegate Initialize()
        {
            var method =
                typeof(ProducerDeliveryHandlerActionIntegration).GetMethod(
                    nameof(WrapWithActivityCompletion),
                    BindingFlags.Static | BindingFlags.NonPublic)!;

            var constructedMethod = method.MakeGenericMethod(typeof(TActionOfDeliveryReport).GetGenericArguments());
#if NET
            return constructedMethod.CreateDelegate<CreateWrapperDelegate>();
#else
            return (CreateWrapperDelegate)constructedMethod.CreateDelegate(typeof(CreateWrapperDelegate));
#endif
        }
    }
}
