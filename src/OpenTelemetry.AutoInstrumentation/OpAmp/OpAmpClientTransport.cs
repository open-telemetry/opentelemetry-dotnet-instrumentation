// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

// Owns access to the upstream client and applies instrumentation suppression to transport operations.
internal sealed class OpAmpClientTransport : IDisposable
{
    private readonly OpAmpClient _client;

    public OpAmpClientTransport(OpAmpClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
        return _client.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using var suppressInstrumentation = SuppressInstrumentationScope.Begin();
        await _client.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _client.Subscribe(listener);
    }

    public void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _client.Unsubscribe(listener);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
