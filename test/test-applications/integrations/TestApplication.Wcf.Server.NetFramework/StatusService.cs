// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ServiceModel;

namespace TestApplication.Wcf.Server.NetFramework;

[ServiceBehavior(
    Namespace = "http://opentelemetry.io/",
    ConcurrencyMode = ConcurrencyMode.Multiple,
    InstanceContextMode = InstanceContextMode.Single,
    UseSynchronizationContext = false,
    Name = "StatusService")]
public class StatusService : IStatusServiceContract
{
    public Task<StatusResponse> PingAsync(StatusRequest request)
    {
        return Task.FromResult(
            new StatusResponse { ServerTime = DateTimeOffset.UtcNow });
    }
}
