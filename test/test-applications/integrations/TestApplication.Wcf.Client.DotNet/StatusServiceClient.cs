// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TestApplication.Wcf.Client.DotNet;

internal sealed class StatusServiceClient : ClientBase<IStatusServiceContract>, IStatusServiceContract
{
    public StatusServiceClient(Binding binding, EndpointAddress remoteAddress)
        : base(binding, remoteAddress)
    {
    }

    public Task<StatusResponse> PingAsync(StatusRequest request)
    {
        return Channel.PingAsync(request);
    }

    public Task OpenAsync()
    {
        ICommunicationObject communicationObject = this;
        return Task.Factory.FromAsync(communicationObject.BeginOpen, communicationObject.EndOpen, null);
    }
}
