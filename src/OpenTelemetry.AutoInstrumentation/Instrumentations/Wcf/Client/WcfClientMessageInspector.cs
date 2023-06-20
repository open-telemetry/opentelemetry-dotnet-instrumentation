// <copyright file="WcfClientMessageInspector.cs" company="OpenTelemetry Authors">
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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Status = OpenTelemetry.Trace.Status;

// source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/06b9a286a6ab2af5257ce26b5dcb6fac56112f96/src/OpenTelemetry.Instrumentation.Wcf

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

/// <summary>
/// Telemetry client message inspector.
/// </summary>
internal class WcfClientMessageInspector : IClientMessageInspector
{
    private readonly Dictionary<string, ActionMetadata> _actionMappings;

    public WcfClientMessageInspector(Dictionary<string, ActionMetadata> actionMappings)
    {
        _actionMappings = actionMappings;
    }

    private static Action<Message, string, string> MessageHeaderValueSetter { get; }
        = (request, name, value) => request.Headers.Add(MessageHeader.CreateHeader(name, "https://www.w3.org/TR/trace-context/", value, false));

    /// <inheritdoc/>
    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        var activity = Activity.Current;
        if (activity is { Source.Name: "OpenTelemetry.AutoInstrumentation.Wcf" })
        {
            string action;
            if (!string.IsNullOrEmpty(request.Headers.Action))
            {
                action = request.Headers.Action;
                activity.DisplayName = action;
            }
            else
            {
                action = string.Empty;
            }

            Propagators.DefaultTextMapPropagator.Inject(
                new PropagationContext(activity.Context, Baggage.Current),
                request,
                MessageHeaderValueSetter);

            if (activity.IsAllDataRequested)
            {
                activity.SetTag(WcfClientConstants.RpcSystemTag, WcfClientConstants.WcfSystemValue);

                if (!_actionMappings.TryGetValue(action, out ActionMetadata actionMetadata))
                {
                    actionMetadata = new ActionMetadata(null, action);
                }

                activity.SetTag(WcfClientConstants.RpcServiceTag, actionMetadata.ContractName);
                activity.SetTag(WcfClientConstants.RpcMethodTag, actionMetadata.OperationName);

                activity.SetTag(WcfClientConstants.SoapMessageVersionTag, request.Version.ToString());

                var remoteAddressUri = request.Headers.To ?? channel.RemoteAddress?.Uri;
                if (remoteAddressUri != null)
                {
                    activity.SetTag(WcfClientConstants.NetPeerNameTag, remoteAddressUri.Host);
                    activity.SetTag(WcfClientConstants.NetPeerPortTag, remoteAddressUri.Port);
                    activity.SetTag(WcfClientConstants.WcfChannelSchemeTag, remoteAddressUri.Scheme);
                    activity.SetTag(WcfClientConstants.WcfChannelPathTag, remoteAddressUri.LocalPath);
                }

                if (request.Properties.Via != null)
                {
                    activity.SetTag(WcfClientConstants.SoapViaTag, request.Properties.Via.ToString());
                }
            }

            return new CorrelationState(activity);
        }

        return new CorrelationState(null);
    }

    /// <inheritdoc />
    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
        var state = correlationState as CorrelationState;
        if (state?.Activity is { IsStopped: false } activity)
        {
            if (activity.IsAllDataRequested)
            {
                if (reply.IsFault)
                {
                    activity.SetStatus(Status.Error);
                }

                activity.SetTag(WcfClientConstants.SoapReplyActionTag, reply.Headers.Action);
            }
        }
    }

    // TODO: inline
    private class CorrelationState
    {
        public CorrelationState(Activity? activity)
        {
            Activity = activity;
        }

        public Activity? Activity { get; }
    }
}

#endif

