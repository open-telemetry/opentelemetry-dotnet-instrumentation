// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;

namespace TestApplication.Wcf.Client.DotNet;

public class StatusServiceClient : ClientBase<IStatusServiceContract>, IStatusServiceContract
{
    public StatusServiceClient(Binding binding, EndpointAddress remoteAddress)
        : base(binding, remoteAddress)
    {
    }

    public Task<StatusResponse> PingAsync(StatusRequest request)
    {
        return this.Channel.PingAsync(request);
    }

    public Task OpenAsync()
    {
        ICommunicationObject communicationObject = this;
        return Task.Factory.FromAsync(communicationObject.BeginOpen, communicationObject.EndOpen, null);
    }
}
