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
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by WCF.
internal sealed class StatusService : IStatusServiceContract
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by WCF.
{
    public Task<StatusResponse> PingAsync(StatusRequest request)
    {
        return Task.FromResult(
            new StatusResponse { ServerTime = DateTimeOffset.UtcNow });
    }
}
