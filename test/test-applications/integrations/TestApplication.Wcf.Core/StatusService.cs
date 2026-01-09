// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Wcf.Core;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. It is created by WCF runtime.
internal sealed class StatusService : IStatusServiceContract
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. It is created by WCF runtime.
{
    public Task<StatusResponse> PingAsync(StatusRequest request)
    {
        return Task.FromResult(
            new StatusResponse { ServerTime = DateTimeOffset.UtcNow });
    }
}
