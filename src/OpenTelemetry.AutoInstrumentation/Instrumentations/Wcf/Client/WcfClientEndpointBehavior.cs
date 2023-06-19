// <copyright file="WcfClientEndpointBehavior.cs" company="OpenTelemetry Authors">
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

using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

// source originated from: https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/06b9a286a6ab2af5257ce26b5dcb6fac56112f96/src/OpenTelemetry.Instrumentation.Wcf

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Wcf.Client;

/// <inheritdoc />
internal class WcfClientEndpointBehavior : IEndpointBehavior
{
    /// <inheritdoc />
    public void Validate(ServiceEndpoint endpoint)
    {
    }

    /// <inheritdoc />
    public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
    {
    }

    /// <inheritdoc />
    public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
    {
    }

    /// <inheritdoc />
    public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
    {
        var actionMappings = new Dictionary<string, ActionMetadata>(StringComparer.OrdinalIgnoreCase);

        foreach (var clientOperation in clientRuntime.ClientOperations)
        {
            actionMappings[clientOperation.Action] = new ActionMetadata($"{clientRuntime.ContractNamespace}{clientRuntime.ContractName}", clientOperation.Name);
        }

        clientRuntime.ClientMessageInspectors.Add(new WcfClientMessageInspector(actionMappings));
    }
}
#endif
