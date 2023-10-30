// <copyright file="ProducerDeliveryHandlerActionIntegration.cs" company="OpenTelemetry Authors">
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
    parameterTypeNames: new[] { ClrNames.String, "!0", "!1", IntegrationConstants.ActionOfDeliveryReportTypeName },
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
                if (deliveryReport.Error.IsError)
                {
                    activity.SetException(new Exception(deliveryReport.Error.ToString()));
                }

                if (deliveryReport.Partition is not null)
                {
                    // Set the final partition message was delivered to.
                    activity.SetTag(MessagingTags.Keys.Kafka.Partition, deliveryReport.Partition.Value);
                }

                if (deliveryReport.Offset is not null)
                {
                    activity.SetTag(MessagingTags.Keys.Kafka.PartitionOffset, deliveryReport.Offset.Value);
                }
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
        private static readonly CreateWrapperDelegate Delegate;

        static WrapperCache()
        {
            var method =
                typeof(ProducerDeliveryHandlerActionIntegration).GetMethod(
                    nameof(WrapWithActivityCompletion),
                    BindingFlags.Static | BindingFlags.NonPublic)!;

            var constructedMethod = method.MakeGenericMethod(typeof(TActionOfDeliveryReport).GetGenericArguments());
            Delegate = (CreateWrapperDelegate)constructedMethod.CreateDelegate(typeof(CreateWrapperDelegate));
        }

        private delegate TActionOfDeliveryReport CreateWrapperDelegate(TActionOfDeliveryReport action, Activity activity);

        public static TActionOfDeliveryReport Create(TActionOfDeliveryReport action, Activity activity)
        {
            return Delegate(action, activity);
        }
    }
}
