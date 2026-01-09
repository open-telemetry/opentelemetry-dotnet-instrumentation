// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using MassTransit;
using TestApplication.MassTransit.Contracts;

namespace TestApplication.MassTransit;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
internal sealed class Worker : BackgroundService
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by MassTransit.
{
    private readonly IBus _bus;

    public Worker(IBus bus)
    {
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _bus.Publish(new TestMessage { Value = $"The time is {DateTimeOffset.Now}" }, stoppingToken).ConfigureAwait(false);

            await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
        }
    }
}
