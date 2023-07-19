// <copyright file="InvokeServiceChannelProxyIntegration.cs" company="OpenTelemetry Authors">
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
#if NETFRAMEWORK
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

/// <summary>
/// InvokeService integration.
/// </summary>
[InstrumentMethod(
    assemblyName: WcfCommonConstants.ServiceModelAssemblyName,
    typeName: WcfClientConstants.ServiceChannelProxyTypeName,
    methodName: WcfClientConstants.InvokeMethodName,
    returnTypeName: WcfClientConstants.MessageTypeName,
    parameterTypeNames: new[] { WcfClientConstants.MessageTypeName },
    minimumVersion: WcfCommonConstants.MinVersion,
    maximumVersion: WcfCommonConstants.MaxVersion,
    integrationName: WcfClientConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class InvokeServiceChannelProxyIntegration
{
    private static object? _serviceEnum;
    private static object? _beginServiceEnum;
    private static object? _taskServiceEnum;

    internal interface IServiceProxyChannel
    {
        IMethodData GetMethodData(object message);
    }

    internal interface IMethodData
    {
        object MethodType { get; }
    }

    private interface IExceptionReturnMessage
    {
        Exception? Exception { get; }
    }

    private interface IReturnMessage
    {
        object ReturnValue { get; }
    }

    private interface ISendAsyncResult
    {
        [DuckField(Name = "exception")]
        Exception? Exception { get; }

        [DuckField(Name = "callback")]
        AsyncCallback? Callback { get; set; }
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TMessage">Type of the message</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="message">Message instance</param>
    /// <returns>CallTarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TMessage>(TTarget instance, TMessage message)
    where TTarget : IServiceProxyChannel
    {
        var methodData = instance.GetMethodData(message!);
        var methodType = methodData.MethodType;

        if (_serviceEnum == null || _beginServiceEnum == null || _taskServiceEnum == null)
        {
            InitializeServiceEnums(methodType);
        }

        if (IsOfSupportedType(methodType))
        {
            var activity = WcfClientCommon.StartActivity();
            return new CallTargetState(activity: activity, methodType);
        }

        return CallTargetState.GetDefault();
    }

    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <param name="returnValue">Return value</param>
    /// <param name="exception">Exception value</param>
    /// <param name="callTargetState">CallTarget state</param>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TReturn">Return type</typeparam>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TReturn returnValue, Exception? exception, in CallTargetState callTargetState)
    {
        var activity = callTargetState.Activity;

        if (activity is null)
        {
            return new CallTargetReturn<TReturn>(returnValue);
        }

        if (exception is not null)
        {
            StopWithException(exception, activity);
            return new CallTargetReturn<TReturn>(returnValue);
        }

        var methodType = callTargetState.State;
        if (methodType is null)
        {
            return new CallTargetReturn<TReturn>(returnValue);
        }

        if (methodType.Equals(_serviceEnum))
        {
            if (returnValue.TryDuckCast<IExceptionReturnMessage>(out var returnMessage))
            {
                CompleteSync(returnMessage.Exception, activity);
            }
        }
        else
        {
            try
            {
                if (!returnValue.TryDuckCast<IReturnMessage>(out var returnMessage))
                {
                    return new CallTargetReturn<TReturn>(returnValue);
                }

                if (methodType.Equals(_taskServiceEnum))
                {
                    AttachActivityCompletionContinuation(returnMessage, activity);
                }
                else if (methodType.Equals(_beginServiceEnum))
                {
                    OverrideCallback(returnMessage.ReturnValue, activity);
                }
            }
            finally
            {
                Activity.Current = activity.Parent;
            }
        }

        return new CallTargetReturn<TReturn>(returnValue);
    }

    private static void OverrideCallback(object sendAsyncResult, Activity activity)
    {
        var duckCastedAsyncResult = DuckCast(sendAsyncResult);

        var initialCallback = duckCastedAsyncResult.Callback;
        duckCastedAsyncResult.Callback = NewCallback;
        void NewCallback(IAsyncResult asyncResult)
        {
            var e = duckCastedAsyncResult.Exception;
            if (e != null)
            {
                activity.SetException(e);
                activity.Stop();
            }

            initialCallback?.Invoke(asyncResult);
        }
    }

    private static ISendAsyncResult DuckCast(object sendAsyncResult)
    {
        // we are interested in private fields of grandparent type
        var targetType = sendAsyncResult.GetType().BaseType.BaseType;
        var createTypeResult = DuckType.CreateCache<ISendAsyncResult>.GetProxy(targetType);
        return createTypeResult.CreateInstance<ISendAsyncResult>(sendAsyncResult);
    }

    private static void StopWithException(Exception exception, Activity activity)
    {
        activity.SetException(exception);
        activity.Stop();
    }

    private static bool IsOfSupportedType(object methodType)
    {
        return methodType.Equals(_serviceEnum) || methodType.Equals(_beginServiceEnum) || methodType.Equals(_taskServiceEnum);
    }

    private static void InitializeServiceEnums(object methodType)
    {
        var type = methodType.GetType();
        _serviceEnum = Enum.Parse(type, "Service");
        _beginServiceEnum = Enum.Parse(type, "BeginService");
        _taskServiceEnum = Enum.Parse(type, "TaskService");
    }

    private static void AttachActivityCompletionContinuation(IReturnMessage returnValue, Activity activity)
    {
        var task = returnValue.ReturnValue as Task;
        task?.ContinueWith(
            (t, a) =>
            {
                var localActivity = a as Activity;
                if (t.Exception is not null)
                {
                    localActivity.SetException(t.Exception.InnerException);
                    localActivity?.Stop();
                }
            },
            activity,
            continuationOptions: TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.HideScheduler);
    }

    private static void CompleteSync(Exception? ex, Activity activity)
    {
        if (ex != null)
        {
            activity.SetException(ex);
            activity.Stop();
        }
    }
}

#endif
